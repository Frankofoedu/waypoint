using System.Threading.Channels;

namespace Tracewire.Application.Services;

public record HitlNotification(Guid EventId, Guid TraceId, string Status, string? Decision, string? BranchName = null, string? Payload = null);

public class HitlNotificationService
{
    private readonly Lock _lock = new();
    private readonly Dictionary<Guid, List<Channel<HitlNotification>>> _traceChannels = [];

    public ChannelReader<HitlNotification> Subscribe(Guid traceId)
    {
        var channel = Channel.CreateBounded<HitlNotification>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });

        lock (_lock)
        {
            if (!_traceChannels.TryGetValue(traceId, out var channels))
            {
                channels = [];
                _traceChannels[traceId] = channels;
            }
            channels.Add(channel);
        }

        return channel.Reader;
    }

    public void Unsubscribe(Guid traceId, ChannelReader<HitlNotification> reader)
    {
        lock (_lock)
        {
            if (!_traceChannels.TryGetValue(traceId, out var channels)) return;
            channels.RemoveAll(c => c.Reader == reader);
            if (channels.Count == 0) _traceChannels.Remove(traceId);
        }
    }

    public void Notify(HitlNotification notification)
    {
        List<Channel<HitlNotification>>? channels;
        lock (_lock)
        {
            if (!_traceChannels.TryGetValue(notification.TraceId, out channels)) return;
            channels = [.. channels];
        }

        foreach (var channel in channels)
        {
            channel.Writer.TryWrite(notification);
        }
    }
}
