from __future__ import annotations

import time
from typing import Any

from tracewire.adapters import TracewireAdapter
from tracewire.trace import TraceContext


class TracewireAutoGenMiddleware:
    """AutoGen middleware adapter.

    Usage:
        from tracewire.adapters.autogen import TracewireAutoGenMiddleware
        middleware = TracewireAutoGenMiddleware(trace_context)
        agent.register_middleware(middleware)
    """

    def __init__(self, ctx: TraceContext):
        self._adapter = TracewireAdapter(ctx)
        self._msg_start_time: float | None = None
        self._tool_start_times: dict[str, float] = {}

    async def on_message(self, message: Any, sender: Any, **kwargs: Any) -> None:
        self._msg_start_time = time.monotonic()
        self._adapter.on_llm_start(str(message), sender=str(sender))

    async def on_response(self, response: Any, **kwargs: Any) -> None:
        latency_ms = None
        if self._msg_start_time is not None:
            latency_ms = int((time.monotonic() - self._msg_start_time) * 1000)
            self._msg_start_time = None
        self._adapter.on_llm_end(str(response), latency_ms=latency_ms)

    async def on_tool_call(self, tool_name: str, arguments: Any, **kwargs: Any) -> None:
        self._tool_start_times[tool_name] = time.monotonic()
        self._adapter.on_tool_start(tool_name, arguments)

    async def on_tool_result(self, tool_name: str, result: Any, **kwargs: Any) -> None:
        latency_ms = None
        if tool_name in self._tool_start_times:
            latency_ms = int((time.monotonic() - self._tool_start_times.pop(tool_name)) * 1000)
        self._adapter.on_tool_end(tool_name, result, latency_ms=latency_ms)
