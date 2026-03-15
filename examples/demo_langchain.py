"""
Waypoint + LangChain — Zero-Touch Instrumentation

Your existing LangChain code stays EXACTLY the same.
You add 2 lines: the trace context and the callback.

Prerequisites:
  docker compose up
  pip install -e "sdk/python[langchain]"

Usage:
  python examples/demo_langchain.py
"""

import asyncio

from waypoint import trace
from waypoint.adapters.langchain import WaypointLangChainCallback

API_KEY = "wp_dev_testkey_123"


async def main():
    # ╔══════════════════════════════════════════════════════╗
    # ║  This is your EXISTING agent code — don't touch it  ║
    # ╚══════════════════════════════════════════════════════╝

    # from langchain_openai import ChatOpenAI
    # from langchain.agents import create_tool_calling_agent, AgentExecutor
    # from langchain.tools import DuckDuckGoSearchRun
    #
    # llm = ChatOpenAI(model="gpt-4")
    # tools = [DuckDuckGoSearchRun()]
    # agent = create_tool_calling_agent(llm, tools, prompt)
    # executor = AgentExecutor(agent=agent, tools=tools)

    # ╔══════════════════════════════════════════════════════╗
    # ║  Add these 2 lines to get full observability        ║
    # ╚══════════════════════════════════════════════════════╝

    async with trace("my-langchain-agent", api_key=API_KEY) as t:
        callback = WaypointLangChainCallback(t)

        # executor.invoke(
        #     {"input": "What are the latest quantum computing breakthroughs?"},
        #     config={"callbacks": [callback]},  # ← just pass the callback
        # )

        # That's it. Every LLM call, tool call, and error is auto-captured.
        # Open http://localhost:5173 to see the full DAG.

        # --- Simulating what LangChain would trigger internally ---
        callback.on_llm_start({"name": "ChatOpenAI"}, ["What are the latest quantum computing breakthroughs?"])
        callback.on_llm_end("I'll search for that information.")
        callback.on_tool_start({"name": "DuckDuckGoSearch"}, "quantum computing breakthroughs 2026")
        callback.on_tool_end("1) Logical qubits at scale  2) Quantum drug discovery  3) Room-temp quantum memory")
        callback.on_llm_start({"name": "ChatOpenAI"}, ["Summarize these results: ..."])
        callback.on_llm_end("Here are the top 3 quantum computing breakthroughs of 2026...")

        print(f"Trace: http://localhost:5173/traces/{t.trace_id}")
        print("6 events auto-captured without touching any agent code.")


if __name__ == "__main__":
    asyncio.run(main())
