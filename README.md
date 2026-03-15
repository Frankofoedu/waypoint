# Waypoint

**Navigate your AI agent's path.**

A developer platform for observing, replaying, and controlling AI agent executions with branching DAG visualization, human-in-the-loop (HITL) support, and side-effect tracking.

## Quick Start

```bash
docker compose up
```

- **API:** http://localhost:5185/swagger
- **Frontend:** http://localhost:5173
- **Health:** http://localhost:5185/v1/health

## Architecture

| Component | Tech | Port |
|-----------|------|------|
| API Server | .NET 9 / ASP.NET Minimal API | 5185 |
| Database | PostgreSQL 16 | 5432 |
| Frontend | React + Vite + React Flow | 5173 |
| Python SDK | httpx + Pydantic | - |
| TypeScript SDK | fetch + TypeScript | - |

## Project Structure

```
waypoint/
├── backend/          .NET 9 API + EF Core + PostgreSQL
├── sdk/python/       Python SDK with framework adapters
├── sdk/typescript/   TypeScript SDK with framework adapters
├── frontend/         React + Vite SPA with DAG visualization
└── docker-compose.yml
```

## Development

### Backend
```bash
cd backend
dotnet run --project src/Waypoint.Api
```

### Frontend
```bash
cd frontend
npm install
npm run dev
```

### Python SDK
```bash
cd sdk/python
pip install -e ".[dev]"
pytest
```

### TypeScript SDK
```bash
cd sdk/typescript
npm install
npm test
```

## SDK Usage

### Python — Instrument an Agent

```python
from waypoint import WaypointClient, trace

client = WaypointClient(base_url="http://localhost:5185", api_key="your-key")

async with trace(client, agent_name="my-agent") as t:
    await t.add_event(event_type="Prompt", payload='{"prompt": "hello"}')
    await t.add_event(event_type="ToolCall", payload='{"tool": "search"}')
```

### Python — LangChain Auto-Instrumentation

```python
from waypoint.adapters.langchain import WaypointCallbackHandler

handler = WaypointCallbackHandler(client=client)
chain.invoke({"input": "hello"}, config={"callbacks": [handler]})
```

### TypeScript — Instrument an Agent

```typescript
import { WaypointClient, trace } from "waypoint-sdk";

const client = new WaypointClient({
  baseUrl: "http://localhost:5185",
  apiKey: "your-key",
});

await trace(client, "my-agent", async (t) => {
  await t.addEvent({ eventType: "Prompt", payload: '{"prompt": "hello"}' });
  await t.addEvent({ eventType: "ToolCall", payload: '{"tool": "search"}' });
});
```
