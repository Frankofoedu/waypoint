from __future__ import annotations

from typing import Any

from waypoint.adapters import WaypointAdapter
from waypoint.trace import TraceContext


class WaypointCrewAICallback:
    """CrewAI step callback adapter.

    Usage:
        from waypoint.adapters.crewai import WaypointCrewAICallback
        callback = WaypointCrewAICallback(trace_context)
        crew = Crew(..., step_callback=callback.on_step)
    """

    def __init__(self, ctx: TraceContext):
        self._adapter = WaypointAdapter.__new__(WaypointAdapter)
        self._adapter._ctx = ctx

    def on_step(self, step_output: Any) -> None:
        step_str = str(step_output)
        if hasattr(step_output, "tool"):
            self._adapter.on_tool_end(step_output.tool, step_str)
        else:
            self._adapter.on_llm_end(step_str)

    def on_task_start(self, task: Any) -> None:
        self._adapter.on_llm_start(str(task))

    def on_task_end(self, task: Any, output: Any) -> None:
        self._adapter.on_llm_end(str(output))
