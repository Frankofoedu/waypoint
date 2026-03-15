# Tracewire TypeScript SDK

TypeScript SDK for [Tracewire](../../README.md) â€” AI agent observability and control.

## Installation

```bash
npm install Tracewire-sdk
```

## Quick Start

```typescript
import { TracewireClient, trace } from "tracewire-sdk";

const client = new TracewireClient({
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
import { TracewireLangChainAdapter } from "Tracewire-sdk/adapters/langchain";

const adapter = new TracewireLangChainAdapter(client);
const callbacks = adapter.createCallbacks();
await chain.invoke({ input: "hello" }, { callbacks });
```

## Development

```bash
npm install
npm test
npm run build
```
