namespace Tracewire.Sdk.Tests;

public class ChatClientAdapterTests
{
    [Fact]
    public async Task CallAsync_LogsPromptAndResponse()
    {
        using var client = new TracewireClient("http://localhost:9999");
        var buffer = new EventBuffer(client, maxSize: 100);
        var traceId = Guid.NewGuid();
        var ctx = new TraceContext(client, buffer, traceId, false);

        var adapter = new Adapters.ChatClientAdapter(ctx, "gpt-4o");

        var result = await adapter.CallAsync("Hello!", prompt =>
            Task.FromResult("Hello back!"));

        Assert.Equal("Hello back!", result);
    }

    [Fact]
    public async Task CallAsync_Generic_ReturnsFullResult()
    {
        using var client = new TracewireClient("http://localhost:9999");
        var buffer = new EventBuffer(client, maxSize: 100);
        var traceId = Guid.NewGuid();
        var ctx = new TraceContext(client, buffer, traceId, false);

        var adapter = new Adapters.ChatClientAdapter(ctx, "gpt-4o");

        var result = await adapter.CallAsync(
            "Hello!",
            prompt => Task.FromResult(new FakeResult("Hello back!", 42)),
            r => r.Text);

        Assert.Equal("Hello back!", result.Text);
        Assert.Equal(42, result.TokenCount);
    }

    private record FakeResult(string Text, int TokenCount);
}
