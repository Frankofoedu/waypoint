from __future__ import annotations

import time
from typing import Any

from tracewire.adapters import TracewireAdapter
from tracewire.trace import TraceContext


class TracewireCrewAICallback:
    """CrewAI step callback adapter.

    Usage:
        from tracewire.adapters.crewai import TracewireCrewAICallback
        callback = TracewireCrewAICallback(trace_context)
        crew = Crew(..., step_callback=callback.on_step)
    """

    def __init__(self, ctx: TraceContext):
        self._adapter = TracewireAdapter(ctx)
        self._task_start_time: float | None = None

    def on_step(self, step_output: Any) -> None:
        step_str = str(step_output)
        if hasattr(step_output, "tool"):
            self._adapter.on_tool_end(step_output.tool, step_str)
        else:
            self._adapter.on_llm_end(step_str)

    def on_task_start(self, task: Any) -> None:
        self._task_start_time = time.monotonic()
        self._adapter.on_llm_start(str(task))

    def on_task_end(self, task: Any, output: Any) -> None:
        latency_ms = None
        if self._task_start_time is not None:
            latency_ms = int((time.monotonic() - self._task_start_time) * 1000)
            self._task_start_time = None
        self._adapter.on_llm_end(str(output), latency_ms=latency_ms)
