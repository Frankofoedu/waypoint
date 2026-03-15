from __future__ import annotations

from typing import Any

from waypoint.adapters import WaypointAdapter
from waypoint.trace import TraceContext


class WaypointAutoGenMiddleware:
    """AutoGen middleware adapter.

    Usage:
        from waypoint.adapters.autogen import WaypointAutoGenMiddleware
        middleware = WaypointAutoGenMiddleware(trace_context)
        agent.register_middleware(middleware)
    """

    def __init__(self, ctx: TraceContext):
        self._adapter = WaypointAdapter.__new__(WaypointAdapter)
        self._adapter._ctx = ctx

    async def on_message(self, message: Any, sender: Any, **kwargs: Any) -> None:
        self._adapter.on_llm_start(str(message), sender=str(sender))

    async def on_response(self, response: Any, **kwargs: Any) -> None:
        self._adapter.on_llm_end(str(response))

    async def on_tool_call(self, tool_name: str, arguments: Any, **kwargs: Any) -> None:
        self._adapter.on_tool_start(tool_name, arguments)

    async def on_tool_result(self, tool_name: str, result: Any, **kwargs: Any) -> None:
        self._adapter.on_tool_end(tool_name, result)
