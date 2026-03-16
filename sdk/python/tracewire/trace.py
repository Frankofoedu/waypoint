from __future__ import annotations

import asyncio
import json
import logging
from contextlib import asynccontextmanager
from typing import Any, AsyncGenerator, Callable, Awaitable
from uuid import UUID

from tracewire.buffer import EventBuffer
from tracewire.client import TracewireClient
from tracewire.models import CreateEventRequest, EventType

logger = logging.getLogger("Tracewire")

ReplayCallback = Callable[[str, str | None, str], Awaitable[None]]


class TraceContext:
    def __init__(self, client: TracewireClient, buffer: EventBuffer, trace_id: UUID, snapshot: bool = False):
        self._client = client
        self._buffer = buffer
        self.trace_id = trace_id
        self._snapshot = snapshot
        self._last_event_id: UUID | None = None
        self._depth = 0
        self._replay_callback: ReplayCallback | None = None
        self._sse_task: asyncio.Task | None = None

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

        try:
            return await asyncio.wait_for(
                self._wait_via_sse(event_id), timeout=timeout
            )
        except asyncio.TimeoutError:
            logger.warning("HITL timeout reached, applying fallback: %s", fallback)
            return fallback

    async def _wait_via_sse(self, event_id: UUID) -> str:
        url = f"{self._client._base_url}/v1/traces/{self.trace_id}/stream"
        params = {"apiKey": self._client._api_key}
        async with self._client._http.stream("GET", url, params=params) as resp:
            buffer = ""
            async for chunk in resp.aiter_text():
                buffer += chunk
                while "\n\n" in buffer:
                    message, buffer = buffer.split("\n\n", 1)
                    for line in message.split("\n"):
                        if line.startswith("data: "):
                            data = json.loads(line[6:])
                            if data.get("eventId") == str(event_id) and data.get("status") == "Resumed":
                                decision_raw = data.get("decision")
                                if decision_raw:
                                    decision = json.loads(decision_raw) if isinstance(decision_raw, str) else decision_raw
                                    return decision.get("decision", "approve")
                                return "approve"
        return "approve"

    def on_replay(self, callback: ReplayCallback) -> None:
        self._replay_callback = callback
        self._sse_task = asyncio.ensure_future(self._listen_for_replays())

    async def _listen_for_replays(self) -> None:
        url = f"{self._client._base_url}/v1/traces/{self.trace_id}/stream"
        params = {"apiKey": self._client._api_key}
        try:
            async with self._client._http.stream("GET", url, params=params) as resp:
                buffer = ""
                async for chunk in resp.aiter_text():
                    buffer += chunk
                    while "\n\n" in buffer:
                        message, buffer = buffer.split("\n\n", 1)
                        for line in message.split("\n"):
                            if not line.startswith("data: "):
                                continue
                            data = json.loads(line[6:])
                            if data.get("status") == "Replay" and self._replay_callback:
                                await self._replay_callback(
                                    data.get("branchName", ""),
                                    data.get("payload"),
                                    data.get("eventId", ""),
                                )
        except Exception:
            logger.debug("Replay SSE listener stopped")

    async def stop_replay_listener(self) -> None:
        if self._sse_task and not self._sse_task.done():
            self._sse_task.cancel()
            try:
                await self._sse_task
            except asyncio.CancelledError:
                pass


@asynccontextmanager
async def trace(
    agent_name: str,
    base_url: str = "http://localhost:5185",
    api_key: str = "",
    metadata: dict | None = None,
    snapshot: bool = False,
) -> AsyncGenerator[TraceContext, None]:
    client = TracewireClient(base_url=base_url, api_key=api_key)
    buffer = EventBuffer(client)
    await buffer.start()
    ctx: TraceContext | None = None

    try:
        trace_resp = await client.create_trace(agent_name, metadata=metadata)
        ctx = TraceContext(client, buffer, trace_resp.id, snapshot=snapshot)
        yield ctx
    except Exception as exc:
        logger.exception("Trace failed: %s", exc)
        raise
    finally:
        if ctx:
            await ctx.stop_replay_listener()
        await buffer.stop()
        await client.close()
