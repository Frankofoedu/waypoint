// Tracewire .NET SDK Example — Simulated AI Agent
//
// Shows how a .NET developer would instrument their agent.
//
// Prerequisites:
//   docker compose up
//   dotnet run --project examples/DemoAgent
//
// Or just: dotnet script examples/demo_agent.csx

using Tracewire.Sdk;

var apiKey = "wp_dev_testkey_123";

await using var t = await TracewireTrace.StartAsync(
    agentName: "research-agent",
    apiKey: apiKey,
    metadata: new Dictionary<string, object>
    {
        ["task"] = "quantum computing research",
        ["model"] = "gpt-4"
    });

Console.WriteLine($"Trace started: {t.TraceId}\n");

// Step 1: User prompt
var prompt = "Research the latest quantum computing breakthroughs";
t.LogEvent(EventType.Prompt, new { role = "user", content = prompt });
Console.WriteLine($"[Prompt]         {prompt}");

// Step 2: LLM plans
await Task.Delay(300); // simulate LLM latency
var plan = "I'll search for recent quantum computing breakthroughs and summarize them.";
t.LogEvent(EventType.ModelResponse, new { role = "assistant", content = plan }, latencyMs: 300, cost: 0.003m);
Console.WriteLine($"[ModelResponse]  {plan}");

// Step 3: Tool call — web search
await Task.Delay(500);
t.LogEvent(EventType.ToolCall, new { tool = "web_search", query = "quantum computing 2026", resultCount = 3 }, latencyMs: 500);
Console.WriteLine($"[ToolCall]       web_search → 3 results");

// Step 4: LLM summarizes
await Task.Delay(400);
var summary = "Top 3: Error-corrected qubits, quantum drug discovery, room-temp quantum memory";
t.LogEvent(EventType.ModelResponse, new { role = "assistant", content = summary }, latencyMs: 400, cost: 0.005m);
Console.WriteLine($"[ModelResponse]  {summary}");

// Step 5: Side-effect — send email
t.RegisterSideEffect("email", new { to = "team@example.com", subject = "Research Summary" });
Console.WriteLine($"[ToolCall]       send_email ⚠️  side-effect registered");

// Step 6: Save to memory
t.LogEvent(EventType.MemoryWrite, new { key = "research:quantum", value = summary });
Console.WriteLine($"[MemoryWrite]    Saved to memory");

Console.WriteLine($"\nTrace complete: {t.TraceId}");
Console.WriteLine($"View DAG:       http://localhost:5173/traces/{t.TraceId}");
