# Tracewire TypeScript SDK

TypeScript SDK for [Tracewire](../../README.md) — AI agent observability and control.

## Installation

```bash
npm install tracewire-sdk
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

## Vercel AI SDK Adapter

Auto-capture all LLM calls when using the Vercel AI SDK (OpenAI, Anthropic, Google, etc.):

```typescript
import { openai } from "@ai-sdk/openai";
import { generateText } from "ai";
import { trace } from "tracewire-sdk";
import { wrapLanguageModel } from "tracewire-sdk/adapters/ai-sdk";

await trace("my-agent", async (t) => {
  const model = wrapLanguageModel(openai("gpt-4o"), t);
  const { text } = await generateText({ model, prompt: "Hello!" });
}, { apiKey: "your-key" });
```

## LangChain Adapter

```typescript
import { createLangChainCallback } from "tracewire-sdk/adapters/langchain";
import { trace } from "tracewire-sdk";

await trace("my-agent", async (t) => {
  const callbacks = [createLangChainCallback(t)];
  await chain.invoke({ input: "hello" }, { callbacks });
}, { apiKey: "your-key" });
```

## Development

```bash
npm install
npm test
npm run build
```
