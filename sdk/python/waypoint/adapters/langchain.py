from __future__ import annotations

from typing import Any

from waypoint.adapters import WaypointAdapter
from waypoint.trace import TraceContext


class WaypointLangChainCallback:
    """LangChain BaseCallbackHandler-compatible adapter.

    Usage:
        from waypoint.adapters.langchain import WaypointLangChainCallback
        callback = WaypointLangChainCallback(trace_context)
        chain.invoke(input, config={"callbacks": [callback]})
    """

    def __init__(self, ctx: TraceContext):
        self._adapter = WaypointAdapter.__new__(WaypointAdapter)
        self._adapter._ctx = ctx

    def on_llm_start(self, serialized: dict[str, Any], prompts: list[str], **kwargs: Any) -> None:
        for prompt in prompts:
            self._adapter.on_llm_start(prompt, serialized_info=str(serialized))

    def on_llm_end(self, response: Any, **kwargs: Any) -> None:
        text = str(response)
        self._adapter.on_llm_end(text)

    def on_llm_error(self, error: BaseException, **kwargs: Any) -> None:
        if isinstance(error, Exception):
            self._adapter.on_error(error)

    def on_tool_start(self, serialized: dict[str, Any], input_str: str, **kwargs: Any) -> None:
        tool_name = serialized.get("name", "unknown")
        self._adapter.on_tool_start(tool_name, input_str)

    def on_tool_end(self, output: str, **kwargs: Any) -> None:
        self._adapter.on_tool_end("unknown", output)

    def on_tool_error(self, error: BaseException, **kwargs: Any) -> None:
        if isinstance(error, Exception):
            self._adapter.on_error(error)

    def on_chain_start(self, serialized: dict[str, Any], inputs: dict[str, Any], **kwargs: Any) -> None:
        pass

    def on_chain_end(self, outputs: dict[str, Any], **kwargs: Any) -> None:
        pass
