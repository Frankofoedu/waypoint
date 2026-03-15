# Waypoint TypeScript SDK

TypeScript SDK for [Waypoint](../../README.md) — AI agent observability and control.

## Installation

```bash
npm install waypoint-sdk
```

## Quick Start

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

## Human-in-the-Loop

```typescript
await trace(client, "my-agent", async (t) => {
  const event = await t.addEvent({
    eventType: "ToolCall",
    payload: '{"tool": "send_email"}',
  });
  const decision = await t.pauseForHuman(event.id, { timeout: 300 });
  if (decision.approved) {
    // proceed with side-effect
  }
});
```

## LangChain Adapter

```typescript
import { WaypointLangChainAdapter } from "waypoint-sdk/adapters/langchain";

const adapter = new WaypointLangChainAdapter(client);
const callbacks = adapter.createCallbacks();
await chain.invoke({ input: "hello" }, { callbacks });
```

## Development

```bash
npm install
npm test
npm run build
```
