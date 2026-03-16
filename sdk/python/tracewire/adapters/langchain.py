from __future__ import annotations

import time
from typing import Any
from uuid import UUID

from tracewire.adapters import TracewireAdapter
from tracewire.trace import TraceContext


class TracewireLangChainCallback:
    """LangChain BaseCallbackHandler-compatible adapter.

    Usage:
        from tracewire.adapters.langchain import TracewireLangChainCallback
        callback = TracewireLangChainCallback(trace_context)
        chain.invoke(input, config={"callbacks": [callback]})
    """

    def __init__(self, ctx: TraceContext):
        self._adapter = TracewireAdapter(ctx)
        self._llm_start_time: float | None = None
        self._tool_names: dict[UUID, str] = {}
        self._tool_start_times: dict[UUID, float] = {}

    def on_llm_start(self, serialized: dict[str, Any], prompts: list[str], **kwargs: Any) -> None:
        self._llm_start_time = time.monotonic()
        for prompt in prompts:
            self._adapter.on_llm_start(prompt, serialized_info=str(serialized))

    def on_llm_end(self, response: Any, **kwargs: Any) -> None:
        latency_ms = None
        if self._llm_start_time is not None:
            latency_ms = int((time.monotonic() - self._llm_start_time) * 1000)
            self._llm_start_time = None
        self._adapter.on_llm_end(str(response), latency_ms=latency_ms)

    def on_llm_error(self, error: BaseException, **kwargs: Any) -> None:
        self._llm_start_time = None
        if isinstance(error, Exception):
            self._adapter.on_error(error)

    def on_tool_start(self, serialized: dict[str, Any], input_str: str, *, run_id: UUID | None = None, **kwargs: Any) -> None:
        tool_name = serialized.get("name", "unknown")
        if run_id:
            self._tool_names[run_id] = tool_name
            self._tool_start_times[run_id] = time.monotonic()
        self._adapter.on_tool_start(tool_name, input_str)

    def on_tool_end(self, output: str, *, run_id: UUID | None = None, **kwargs: Any) -> None:
        tool_name = self._tool_names.pop(run_id, "unknown") if run_id else "unknown"
        latency_ms = None
        if run_id and run_id in self._tool_start_times:
            latency_ms = int((time.monotonic() - self._tool_start_times.pop(run_id)) * 1000)
        self._adapter.on_tool_end(tool_name, output, latency_ms=latency_ms)

    def on_tool_error(self, error: BaseException, *, run_id: UUID | None = None, **kwargs: Any) -> None:
        if run_id:
            self._tool_names.pop(run_id, None)
            self._tool_start_times.pop(run_id, None)
        if isinstance(error, Exception):
            self._adapter.on_error(error)

    def on_chain_start(self, serialized: dict[str, Any], inputs: dict[str, Any], **kwargs: Any) -> None:
        pass

    def on_chain_end(self, outputs: dict[str, Any], **kwargs: Any) -> None:
        pass
