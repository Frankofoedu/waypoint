from __future__ import annotations

import asyncio
import logging
import threading
from collections import deque
from typing import Any

from waypoint.client import WaypointClient
from waypoint.models import CreateEventRequest

logger = logging.getLogger("waypoint")


class EventBuffer:
    def __init__(self, client: WaypointClient, max_size: int = 1000, flush_interval: float = 1.0):
        self._client = client
        self._max_size = max_size
        self._flush_interval = flush_interval
        self._buffer: deque[CreateEventRequest] = deque(maxlen=max_size)
        self._lock = threading.Lock()
        self._flush_task: asyncio.Task | None = None
        self._running = False

    def enqueue(self, event: CreateEventRequest) -> None:
        with self._lock:
            if len(self._buffer) >= self._max_size:
                logger.warning("Event buffer full, dropping oldest event")
            self._buffer.append(event)

    async def start(self) -> None:
        self._running = True
        self._flush_task = asyncio.create_task(self._flush_loop())

    async def stop(self) -> None:
        self._running = False
        if self._flush_task:
            self._flush_task.cancel()
            try:
                await self._flush_task
            except asyncio.CancelledError:
                pass
        await self._flush_remaining()

    async def _flush_loop(self) -> None:
        while self._running:
            await asyncio.sleep(self._flush_interval)
            await self._flush_remaining()

    async def _flush_remaining(self) -> None:
        events: list[CreateEventRequest] = []
        with self._lock:
            while self._buffer:
                events.append(self._buffer.popleft())

        for event in events:
            try:
                await self._client.create_event(event)
            except Exception:
                logger.exception("Failed to flush event %s", event.event_type)
