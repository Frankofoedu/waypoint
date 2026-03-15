using System.Text.Json;

namespace Waypoint.Sdk;

public sealed class TraceContext : IAsyncDisposable
{
    private readonly WaypointClient _client;
    private readonly EventBuffer _buffer;
    private readonly bool _snapshot;
    private int _depth;

    public Guid TraceId { get; }

    internal TraceContext(WaypointClient client, EventBuffer buffer, Guid traceId, bool snapshot)
    {
        _client = client;
        _buffer = buffer;
        TraceId = traceId;
        _snapshot = snapshot;
    }

    public void LogEvent(
        EventType eventType,
        object? payload = null,
        int? latencyMs = null,
        decimal? cost = null,
        object? stateSnapshot = null,
        object[]? sideEffects = null)
    {
        var evt = new CreateEventRequest(
            TraceId: TraceId,
            EventType: eventType,
            Depth: _depth,
            Payload: payload is not null ? JsonSerializer.Serialize(payload) : null,
            LatencyMs: latencyMs,
            Cost: cost,
            StateSnapshot: stateSnapshot is not null ? JsonSerializer.Serialize(stateSnapshot) : null,
            SideEffects: sideEffects is not null ? JsonSerializer.Serialize(sideEffects) : null);

        _buffer.Enqueue(evt);
        _depth++;
    }

    public void RegisterSideEffect(string effectType, object? details = null)
    {
        LogEvent(
            EventType.ToolCall,
            payload: new { type = "side_effect", effectType },
            sideEffects: [new { type = effectType, details = details ?? new { } }]);
    }

    public async Task<string> PauseForHumanAsync(int timeoutSeconds = 60, string fallback = "abort")
    {
        await _buffer.StopAsync();
        _buffer.Start();
        await Task.Delay(500);

        var traceData = await _client.GetTraceAsync(TraceId);
        var lastEvent = traceData.Events.OrderByDescending(e => e.StepOrder).FirstOrDefault();
        if (lastEvent is null) return fallback;

        await _client.PauseEventAsync(lastEvent.Id, timeoutSeconds);

        var deadline = DateTime.UtcNow.AddSeconds(timeoutSeconds);
        while (DateTime.UtcNow < deadline)
        {
            var trace = await _client.GetTraceAsync(TraceId);
            var evt = trace.Events.FirstOrDefault(e => e.Id == lastEvent.Id);
            if (evt?.HitlStatus == HitlStatus.Resumed)
            {
                if (evt.HitlDecision is not null)
                {
                    var doc = JsonDocument.Parse(evt.HitlDecision);
                    if (doc.RootElement.TryGetProperty("decision", out var dec))
                        return dec.GetString() ?? "approve";
                }
                return "approve";
            }
            await Task.Delay(2000);
        }

        return fallback;
    }

    public async ValueTask DisposeAsync()
    {
        await _buffer.StopAsync();
    }
}

public static class WaypointTrace
{
    public static async Task<TraceContext> StartAsync(
        string agentName,
        string baseUrl = "http://localhost:5185",
        string apiKey = "",
        Dictionary<string, object>? metadata = null,
        bool snapshot = false)
    {
        var client = new WaypointClient(baseUrl, apiKey);
        var buffer = new EventBuffer(client);
        buffer.Start();

        var metadataJson = metadata is not null ? JsonSerializer.Serialize(metadata) : null;
        var trace = await client.CreateTraceAsync(agentName, metadataJson);
        return new TraceContext(client, buffer, trace.Id, snapshot);
    }
}
