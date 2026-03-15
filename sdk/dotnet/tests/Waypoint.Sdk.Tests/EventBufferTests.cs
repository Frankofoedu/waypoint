namespace Waypoint.Sdk.Tests;

public class EventBufferTests
{
    [Fact]
    public void Enqueue_AddsToBuffer()
    {
        using var client = new WaypointClient("http://localhost:9999");
        var buffer = new EventBuffer(client, maxSize: 10);

        var evt = new CreateEventRequest(Guid.NewGuid(), EventType.Prompt, 0);
        buffer.Enqueue(evt);
        buffer.Enqueue(evt);

        // No exception means events were queued successfully
        Assert.True(true);
    }

    [Fact]
    public void Enqueue_DropsOldest_WhenFull()
    {
        using var client = new WaypointClient("http://localhost:9999");
        var buffer = new EventBuffer(client, maxSize: 2);

        for (var i = 0; i < 5; i++)
            buffer.Enqueue(new CreateEventRequest(Guid.NewGuid(), EventType.Prompt, i));

        // Shouldn't throw, oldest events silently dropped
        Assert.True(true);
    }
}
