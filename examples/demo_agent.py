"""
Tracewire SDK Example — Simulated AI Agent

Shows how a developer would instrument their agent with Tracewire.

Prerequisites:
  docker compose up          # Start the API + PostgreSQL
  pip install -e sdk/python  # Install the Python SDK

Usage:
  python examples/demo_agent.py
"""

import asyncio
import time

from tracewire import trace, EventType

API_URL = "http://localhost:5185"
API_KEY = "wp_dev_testkey_123"


# --- Simulated agent functions (replace with your real logic) ---

def call_llm(prompt: str) -> str:
    """Simulate an LLM call."""
    time.sleep(0.3)
    return "I'll search for recent quantum computing breakthroughs and summarize them."


def search_web(query: str) -> list[dict]:
    """Simulate a web search tool."""
    time.sleep(0.5)
    return [
        {"title": "Error-corrected logical qubits at scale", "url": "https://example.com/1"},
        {"title": "Quantum advantage in drug discovery", "url": "https://example.com/2"},
        {"title": "Room-temperature quantum memory", "url": "https://example.com/3"},
    ]


def summarize(results: list[dict]) -> str:
    """Simulate an LLM summarization call."""
    time.sleep(0.4)
    titles = [r["title"] for r in results]
    return f"Top 3 breakthroughs: {', '.join(titles)}"


def send_email(to: str, subject: str, body: str) -> None:
    """Simulate sending an email (side-effect!)."""
    time.sleep(0.1)


# --- The actual instrumented agent ---

async def run_agent():
    async with trace(
        agent_name="research-agent",
        base_url=API_URL,
        api_key=API_KEY,
        metadata={"task": "quantum computing research", "model": "gpt-4"},
    ) as t:
        print(f"Trace started: {t.trace_id}\n")

        # Step 1: User prompt
        user_prompt = "Research the latest quantum computing breakthroughs"
        t.log_event(EventType.PROMPT, payload={"role": "user", "content": user_prompt})
        print(f"[Prompt]         {user_prompt}")

        # Step 2: LLM decides what to do
        start = time.monotonic()
        plan = call_llm(user_prompt)
        t.log_event(
            EventType.MODEL_RESPONSE,
            payload={"role": "assistant", "content": plan},
            latency_ms=int((time.monotonic() - start) * 1000),
            cost=0.003,
        )
        print(f"[ModelResponse]  {plan}")

        # Step 3: Agent calls a tool
        start = time.monotonic()
        results = search_web("quantum computing breakthroughs 2026")
        t.log_event(
            EventType.TOOL_CALL,
            payload={"tool": "web_search", "query": "quantum computing breakthroughs 2026", "result_count": len(results)},
            latency_ms=int((time.monotonic() - start) * 1000),
        )
        print(f"[ToolCall]       web_search → {len(results)} results")

        # Step 4: LLM summarizes
        start = time.monotonic()
        summary = summarize(results)
        t.log_event(
            EventType.MODEL_RESPONSE,
            payload={"role": "assistant", "content": summary},
            latency_ms=int((time.monotonic() - start) * 1000),
            cost=0.005,
        )
        print(f"[ModelResponse]  {summary}")

        # Step 5: Agent sends email (side-effect → flagged for replay warnings!)
        start = time.monotonic()
        send_email("team@example.com", "Research Summary", summary)
        t.register_side_effect("email", {"to": "team@example.com", "subject": "Research Summary"})
        print(f"[ToolCall]       send_email ⚠️  side-effect registered")

        # Step 6: Agent saves to memory
        t.log_event(
            EventType.MEMORY_WRITE,
            payload={"key": "research:quantum", "value": summary},
        )
        print(f"[MemoryWrite]    Saved research summary to memory")

        print(f"\nTrace complete: {t.trace_id}")
        print(f"View DAG:       http://localhost:5173/traces/{t.trace_id}")


if __name__ == "__main__":
    asyncio.run(run_agent())
