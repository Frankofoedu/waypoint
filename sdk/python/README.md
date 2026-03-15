# Tracewire Python SDK

Python SDK for [Tracewire](../../README.md) — AI agent observability and control.

## Installation

```bash
pip install -e "."
```

With framework adapters:
```bash
pip install -e ".[langchain]"   # LangChain adapter
pip install -e ".[dev]"         # Development dependencies
```

## Quick Start

```python
from tracewire import TracewireClient, trace

client = TracewireClient(base_url="http://localhost:5185", api_key="your-key")

async with trace(client, agent_name="my-agent") as t:
    await t.add_event(event_type="Prompt", payload='{"prompt": "hello"}')
    await t.add_event(event_type="ToolCall", payload='{"tool": "search"}')
```

## Human-in-the-Loop

```python
async with trace(client, agent_name="my-agent") as t:
    event = await t.add_event(event_type="ToolCall", payload='{"tool": "send_email"}')
    decision = await t.pause_for_human(event_id=event.id, timeout=300)
    if decision.approved:
        # proceed with side-effect
        pass
```

## LangChain Adapter

```python
from tracewire.adapters.langchain import TracewireCallbackHandler

handler = TracewireCallbackHandler(client=client)
chain.invoke({"input": "hello"}, config={"callbacks": [handler]})
```

## Framework Adapters

| Adapter | Import |
|---------|--------|
| LangChain | `tracewire.adapters.langchain.TracewireCallbackHandler` |
| AutoGen | `tracewire.adapters.autogen.TracewireAutoGenMiddleware` |
| CrewAI | `tracewire.adapters.crewai.TracewireCrewCallback` |

## Development

```bash
pip install -e ".[dev]"
pytest
```
