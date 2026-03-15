using System.Text.Json;

namespace Waypoint.Sdk.Tests;

public class ModelsTests
{
    [Fact]
    public void TraceStatus_SerializesAsString()
    {
        var json = JsonSerializer.Serialize(TraceStatus.Running);
        Assert.Equal("\"Running\"", json);
    }

    [Fact]
    public void EventType_SerializesAsString()
    {
        var json = JsonSerializer.Serialize(EventType.ToolCall);
        Assert.Equal("\"ToolCall\"", json);
    }

    [Fact]
    public void CreateTraceRequest_SerializesWithCamelCase()
    {
        var req = new CreateTraceRequest("my-agent", """{"key":"value"}""");
        var json = JsonSerializer.Serialize(req);
        Assert.Contains("\"agentName\":\"my-agent\"", json);
        Assert.Contains("\"metadata\":", json);
    }

    [Fact]
    public void CreateEventRequest_OmitsNullFields()
    {
        var req = new CreateEventRequest(Guid.NewGuid(), EventType.Prompt, 0);
        var opts = new JsonSerializerOptions { DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull };
        var json = JsonSerializer.Serialize(req, opts);
        Assert.DoesNotContain("parentId", json);
        Assert.DoesNotContain("payload", json);
    }
}
