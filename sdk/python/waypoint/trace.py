from __future__ import annotations

import asyncio
import json
import logging
import time
from contextlib import asynccontextmanager
from typing import Any, AsyncGenerator
from uuid import UUID

from waypoint.buffer import EventBuffer
from waypoint.client import WaypointClient
from waypoint.models import CreateEventRequest, EventType, HitlStatus

logger = logging.getLogger("waypoint")


class TraceContext:
    def __init__(self, client: WaypointClient, buffer: EventBuffer, trace_id: UUID, snapshot: bool = False):
        self._client = client
        self._buffer = buffer
        self.trace_id = trace_id
        self._snapshot = snapshot
        self._last_event_id: UUID | None = None
        self._depth = 0

    def log_event(
        self,
        event_type: EventType,
        payload: dict | None = None,
        latency_ms: int | None = None,
        cost: float | None = None,
        state_snapshot: dict | None = None,
        side_effects: list[dict] | None = None,
    ) -> None:
        event = CreateEventRequest(
            trace_id=self.trace_id,
            parent_id=self._last_event_id,
            event_type=event_type,
            payload=json.dumps(payload) if payload else None,
            latency_ms=latency_ms,
            cost=cost,
            depth=self._depth,
            state_snapshot=json.dumps(state_snapshot) if (state_snapshot or self._snapshot) and state_snapshot else None,
            side_effects=json.dumps(side_effects) if side_effects else None,
        )
        self._buffer.enqueue(event)
        self._depth += 1

    def register_side_effect(self, effect_type: str, details: dict | None = None) -> None:
        self.log_event(
            EventType.TOOL_CALL,
            payload={"type": "side_effect", "effect_type": effect_type},
            side_effects=[{"type": effect_type, "details": details or {}}],
        )

    async def pause_for_human(self, timeout: int = 60, fallback: str = "abort") -> str:
        if self._last_event_id is None:
            self.log_event(EventType.PROMPT, payload={"type": "hitl_pause"})
            await self._buffer.stop()
            await self._buffer.start()
            await asyncio.sleep(0.5)

        event_id = self._last_event_id
        if event_id is None:
            logger.warning("No event to pause on, applying fallback: %s", fallback)
            return fallback

        await self._client.pause_event(event_id, timeout)

        deadline = time.monotonic() + timeout
        while time.monotonic() < deadline:
            trace_data = await self._client.get_trace(self.trace_id)
            for event in trace_data.get("events", []):
                if event.get("id") == str(event_id) and event.get("hitlStatus") == HitlStatus.RESUMED:
                    decision_raw = event.get("hitlDecision")
                    if decision_raw:
                        decision = json.loads(decision_raw) if isinstance(decision_raw, str) else decision_raw
                        return decision.get("decision", "approve")
                    return "approve"
            await asyncio.sleep(2)

        logger.warning("HITL timeout reached, applying fallback: %s", fallback)
        return fallback


@asynccontextmanager
async def trace(
    agent_name: str,
    base_url: str = "http://localhost:5185",
    api_key: str = "",
    snapshot: bool = False,
) -> AsyncGenerator[TraceContext, None]:
    client = WaypointClient(base_url=base_url, api_key=api_key)
    buffer = EventBuffer(client)
    await buffer.start()

    try:
        trace_resp = await client.create_trace(agent_name)
        ctx = TraceContext(client, buffer, trace_resp.id, snapshot=snapshot)
        yield ctx
    except Exception as exc:
        logger.exception("Trace failed: %s", exc)
        raise
    finally:
        await buffer.stop()
        await client.close()
