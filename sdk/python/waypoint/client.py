from __future__ import annotations

import json
import logging
from uuid import UUID

import httpx

from waypoint.models import (
    CreateEventRequest,
    CreateTraceRequest,
    EventResponse,
    TraceResponse,
)

logger = logging.getLogger("waypoint")


class WaypointClient:
    def __init__(self, base_url: str = "http://localhost:5185", api_key: str = ""):
        self._base_url = base_url.rstrip("/")
        self._api_key = api_key
        self._http = httpx.AsyncClient(
            base_url=self._base_url,
            headers={"X-API-Key": api_key},
            timeout=10.0,
        )

    async def create_trace(self, agent_name: str, metadata: dict | None = None) -> TraceResponse:
        payload = CreateTraceRequest(
            agent_name=agent_name,
            metadata=json.dumps(metadata) if metadata else None,
        )
        resp = await self._http.post("/v1/traces", json=payload.model_dump(by_alias=True))
        resp.raise_for_status()
        return TraceResponse.model_validate(resp.json())

    async def create_event(self, request: CreateEventRequest) -> EventResponse:
        resp = await self._http.post("/v1/events", json=request.model_dump(by_alias=True, exclude_none=True))
        resp.raise_for_status()
        return EventResponse.model_validate(resp.json())

    async def pause_event(self, event_id: UUID, timeout_seconds: int = 60) -> None:
        resp = await self._http.post(
            f"/v1/events/{event_id}/pause",
            json={"timeoutSeconds": timeout_seconds},
        )
        resp.raise_for_status()

    async def resume_event(self, event_id: UUID, decision: str, comments: str | None = None) -> None:
        resp = await self._http.post(
            f"/v1/events/{event_id}/resume",
            json={"decision": decision, "comments": comments},
        )
        resp.raise_for_status()

    async def get_trace(self, trace_id: UUID) -> dict:
        resp = await self._http.get(f"/v1/traces/{trace_id}")
        resp.raise_for_status()
        return resp.json()

    async def close(self) -> None:
        await self._http.aclose()
