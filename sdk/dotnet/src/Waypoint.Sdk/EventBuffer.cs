using System.Collections.Concurrent;

namespace Waypoint.Sdk;

public sealed class EventBuffer : IAsyncDisposable
{
    private readonly WaypointClient _client;
    private readonly ConcurrentQueue<CreateEventRequest> _queue = new();
    private readonly int _maxSize;
    private readonly TimeSpan _flushInterval;
    private CancellationTokenSource? _cts;
    private Task? _flushTask;
    private int _count;

    public EventBuffer(WaypointClient client, int maxSize = 1000, TimeSpan? flushInterval = null)
    {
        _client = client;
        _maxSize = maxSize;
        _flushInterval = flushInterval ?? TimeSpan.FromSeconds(1);
    }

    public void Enqueue(CreateEventRequest evt)
    {
        if (Interlocked.Increment(ref _count) > _maxSize)
        {
            _queue.TryDequeue(out _);
            Interlocked.Decrement(ref _count);
        }
        _queue.Enqueue(evt);
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _flushTask = FlushLoopAsync(_cts.Token);
    }

    public async Task StopAsync()
    {
        if (_cts is not null)
        {
            await _cts.CancelAsync();
            try { if (_flushTask is not null) await _flushTask; }
            catch (OperationCanceledException) { }
        }
        await FlushRemainingAsync();
    }

    private async Task FlushLoopAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            await Task.Delay(_flushInterval, ct);
            await FlushRemainingAsync();
        }
    }

    private async Task FlushRemainingAsync()
    {
        while (_queue.TryDequeue(out var evt))
        {
            Interlocked.Decrement(ref _count);
            try { await _client.CreateEventAsync(evt); }
            catch { /* log in production */ }
        }
    }

    public async ValueTask DisposeAsync()
    {
        await StopAsync();
        _cts?.Dispose();
    }
}
