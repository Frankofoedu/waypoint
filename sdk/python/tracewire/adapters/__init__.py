from __future__ import annotations

from typing import Any

from tracewire.models import EventType
from tracewire.trace import TraceContext


class TracewireAdapter:
    def __init__(self, ctx: TraceContext):
        self._ctx = ctx

    def on_llm_start(self, prompt: str, **kwargs: Any) -> None:
        self._ctx.log_event(
            EventType.PROMPT,
            payload={"prompt": prompt, **kwargs},
        )

    def on_llm_end(self, response: str, latency_ms: int | None = None, cost: float | None = None, **kwargs: Any) -> None:
        self._ctx.log_event(
            EventType.MODEL_RESPONSE,
            payload={"response": response, **kwargs},
            latency_ms=latency_ms,
            cost=cost,
        )

    def on_tool_start(self, tool_name: str, input_data: Any = None, **kwargs: Any) -> None:
        self._ctx.log_event(
            EventType.TOOL_CALL,
            payload={"tool": tool_name, "input": input_data, **kwargs},
        )

    def on_tool_end(self, tool_name: str, output: Any = None, latency_ms: int | None = None, **kwargs: Any) -> None:
        self._ctx.log_event(
            EventType.TOOL_CALL,
            payload={"tool": tool_name, "output": output, "completed": True, **kwargs},
            latency_ms=latency_ms,
        )

    def on_error(self, error: Exception, **kwargs: Any) -> None:
        self._ctx.log_event(
            EventType.ERROR,
            payload={"error": str(error), "type": type(error).__name__, **kwargs},
        )
