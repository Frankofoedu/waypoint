namespace Waypoint.Sdk.Adapters;

public abstract class WaypointAdapter
{
    protected readonly TraceContext Ctx;

    protected WaypointAdapter(TraceContext ctx) => Ctx = ctx;

    public virtual void OnLlmStart(string prompt, string? model = null) =>
        Ctx.LogEvent(EventType.Prompt, new { prompt, model });

    public virtual void OnLlmEnd(string response, int? latencyMs = null, decimal? cost = null) =>
        Ctx.LogEvent(EventType.ModelResponse, new { response }, latencyMs, cost);

    public virtual void OnToolStart(string toolName, object? input = null) =>
        Ctx.LogEvent(EventType.ToolCall, new { tool = toolName, input });

    public virtual void OnToolEnd(string toolName, object? output = null, int? latencyMs = null) =>
        Ctx.LogEvent(EventType.ToolCall, new { tool = toolName, output, completed = true }, latencyMs);

    public virtual void OnError(Exception error) =>
        Ctx.LogEvent(EventType.Error, new { error = error.Message, type = error.GetType().Name });
}
