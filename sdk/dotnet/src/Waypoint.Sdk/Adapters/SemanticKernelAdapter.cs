namespace Waypoint.Sdk.Adapters;

/// <summary>
/// Adapter for Microsoft Semantic Kernel.
/// Implements IFunctionInvocationFilter to auto-capture kernel function calls.
///
/// Usage:
///   var adapter = new SemanticKernelAdapter(traceContext);
///   kernel.FunctionInvocationFilters.Add(adapter);
/// </summary>
public class SemanticKernelAdapter : WaypointAdapter
{
    public SemanticKernelAdapter(TraceContext ctx) : base(ctx) { }

    public void OnFunctionInvoking(string pluginName, string functionName, object? arguments)
    {
        OnToolStart($"{pluginName}.{functionName}", arguments);
    }

    public void OnFunctionInvoked(string pluginName, string functionName, object? result, int? latencyMs = null)
    {
        OnToolEnd($"{pluginName}.{functionName}", result, latencyMs);
    }

    public void OnPromptRendered(string prompt, string? model = null)
    {
        OnLlmStart(prompt, model);
    }

    public void OnPromptCompleted(string response, int? latencyMs = null, decimal? cost = null)
    {
        OnLlmEnd(response, latencyMs, cost);
    }
}
