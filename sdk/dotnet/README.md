# Waypoint .NET SDK

.NET SDK for [Waypoint](../../README.md) — AI agent observability and control.

## Installation

Add a project reference (NuGet package coming soon):

```bash
dotnet add reference path/to/sdk/dotnet/src/Waypoint.Sdk
```

## Quick Start — Manual Instrumentation

For any LLM client (OpenAI, Anthropic, Azure OpenAI, local models):

```csharp
using Waypoint.Sdk;

await using var t = await WaypointTrace.StartAsync(
    agentName: "my-agent",
    apiKey: "wp_dev_testkey_123");

// Your existing code — call any LLM
var response = await openAiClient.GetChatCompletionAsync(prompt);

// Log what happened
t.LogEvent(EventType.Prompt, new { role = "user", content = prompt });
t.LogEvent(EventType.ModelResponse, new { content = response }, latencyMs: 450, cost: 0.003m);

// Flag side-effects
t.RegisterSideEffect("email", new { to = "team@acme.com" });
```

## Zero-Touch — Semantic Kernel

```csharp
using Waypoint.Sdk;
using Waypoint.Sdk.Adapters;

await using var t = await WaypointTrace.StartAsync("my-sk-agent", apiKey: "wp_dev_testkey_123");
var adapter = new SemanticKernelAdapter(t);

// Hook into Semantic Kernel — your kernel code stays unchanged
adapter.OnPromptRendered("What is quantum computing?", "gpt-4");
// ... kernel invokes functions automatically ...
adapter.OnFunctionInvoking("SearchPlugin", "Search", new { query = "quantum" });
adapter.OnFunctionInvoked("SearchPlugin", "Search", results, latencyMs: 200);
adapter.OnPromptCompleted("Here are the results...", latencyMs: 1200, cost: 0.005m);
```

## Human-in-the-Loop

```csharp
await using var t = await WaypointTrace.StartAsync("my-agent", apiKey: "key");

t.LogEvent(EventType.ToolCall, new { tool = "deploy_production" });
var decision = await t.PauseForHumanAsync(timeoutSeconds: 120, fallback: "abort");

if (decision == "approve")
    await DeployAsync();
```

## Development

```bash
cd sdk/dotnet
dotnet test
```
