using System.Text.Json;

namespace Tracewire.Sdk;

public sealed class TraceContext : IAsyncDisposable
{
    private readonly TracewireClient _client;
    private readonly EventBuffer _buffer;
    private readonly bool _snapshot;
    private int _depth;
    private CancellationTokenSource? _replayCts;
    private Task? _replayTask;

    public Guid TraceId { get; }

    internal TraceContext(TracewireClient client, EventBuffer buffer, Guid traceId, bool snapshot)
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

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds));
        try
        {
            return await WaitViaSseAsync(lastEvent.Id, cts.Token);
        }
        catch (OperationCanceledException)
        {
            return fallback;
        }
    }

    private async Task<string> WaitViaSseAsync(Guid eventId, CancellationToken ct)
    {
        var url = $"{_client.BaseUrl}/v1/traces/{TraceId}/stream?apiKey={Uri.EscapeDataString(_client.ApiKey)}";
        using var http = new HttpClient();
        using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        resp.EnsureSuccessStatusCode();

        using var stream = await resp.Content.ReadAsStreamAsync(ct);
        using var reader = new StreamReader(stream);
        var buffer = "";

        while (!ct.IsCancellationRequested)
        {
            var chunk = new char[4096];
            var read = await reader.ReadAsync(chunk.AsMemory(), ct);
            if (read == 0) break;
            buffer += new string(chunk, 0, read);

            while (buffer.Contains("\n\n"))
            {
                var idx = buffer.IndexOf("\n\n", StringComparison.Ordinal);
                var message = buffer[..idx];
                buffer = buffer[(idx + 2)..];

                foreach (var line in message.Split('\n'))
                {
                    if (!line.StartsWith("data: ")) continue;
                    var json = line[6..];
                    var doc = JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    if (root.TryGetProperty("eventId", out var eid) &&
                        eid.GetString() == eventId.ToString() &&
                        root.TryGetProperty("status", out var status) &&
                        status.GetString() == "Resumed")
                    {
                        if (root.TryGetProperty("decision", out var decisionRaw) &&
                            decisionRaw.ValueKind == JsonValueKind.String)
                        {
                            var decDoc = JsonDocument.Parse(decisionRaw.GetString()!);
                            if (decDoc.RootElement.TryGetProperty("decision", out var dec))
                                return dec.GetString() ?? "approve";
                        }
                        return "approve";
                    }
                }
            }
        }

        throw new OperationCanceledException();
    }

    public void OnReplay(Func<string, string?, string, Task> callback)
    {
        _replayCts = new CancellationTokenSource();
        _replayTask = ListenForReplaysAsync(callback, _replayCts.Token);
    }

    private async Task ListenForReplaysAsync(Func<string, string?, string, Task> callback, CancellationToken ct)
    {
        var url = $"{_client.BaseUrl}/v1/traces/{TraceId}/stream?apiKey={Uri.EscapeDataString(_client.ApiKey)}";
        using var http = new HttpClient();
        try
        {
            using var resp = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            resp.EnsureSuccessStatusCode();

            using var stream = await resp.Content.ReadAsStreamAsync(ct);
            using var reader = new StreamReader(stream);
            var buffer = "";

            while (!ct.IsCancellationRequested)
            {
                var chunk = new char[4096];
                var read = await reader.ReadAsync(chunk.AsMemory(), ct);
                if (read == 0) break;
                buffer += new string(chunk, 0, read);

                while (buffer.Contains("\n\n"))
                {
                    var idx = buffer.IndexOf("\n\n", StringComparison.Ordinal);
                    var message = buffer[..idx];
                    buffer = buffer[(idx + 2)..];

                    foreach (var line in message.Split('\n'))
                    {
                        if (!line.StartsWith("data: ")) continue;
                        var json = line[6..];
                        var doc = JsonDocument.Parse(json);
                        var root = doc.RootElement;

                        if (root.TryGetProperty("status", out var status) &&
                            status.GetString() == "Replay")
                        {
                            var branchName = root.TryGetProperty("branchName", out var bn) ? bn.GetString() ?? "" : "";
                            var payload = root.TryGetProperty("payload", out var pl) ? pl.GetString() : null;
                            var eventId = root.TryGetProperty("eventId", out var eid) ? eid.GetString() ?? "" : "";
                            await callback(branchName, payload, eventId);
                        }
                    }
                }
            }
        }
        catch (OperationCanceledException) { }
    }

    public async ValueTask DisposeAsync()
    {
        if (_replayCts is not null)
        {
            await _replayCts.CancelAsync();
            if (_replayTask is not null)
                try { await _replayTask; } catch (OperationCanceledException) { }
            _replayCts.Dispose();
        }
        await _buffer.StopAsync();
    }
}

public static class TracewireTrace
{
    public static async Task<TraceContext> StartAsync(
        string agentName,
        string baseUrl = "http://localhost:5185",
        string apiKey = "",
        Dictionary<string, object>? metadata = null,
        bool snapshot = false)
    {
        var client = new TracewireClient(baseUrl, apiKey);
        var buffer = new EventBuffer(client);
        buffer.Start();

        var metadataJson = metadata is not null ? JsonSerializer.Serialize(metadata) : null;
        var trace = await client.CreateTraceAsync(agentName, metadataJson);
        return new TraceContext(client, buffer, trace.Id, snapshot);
    }
}
