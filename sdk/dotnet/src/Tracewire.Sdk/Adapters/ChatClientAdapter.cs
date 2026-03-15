using System.Diagnostics;
using System.Text.Json;

namespace Tracewire.Sdk.Adapters;

/// <summary>
/// Wraps any LLM call to auto-capture prompts, responses, and errors.
/// Works with OpenAI, Azure OpenAI, Ollama, and any provider.
///
/// Usage:
///   await using var t = await TracewireTrace.StartAsync("my-agent", apiKey: "key");
///   var llm = new ChatClientAdapter(t, "gpt-4o");
///
///   var response = await llm.CallAsync("Hello!", async prompt =>
///   {
///       var result = await openAiClient.CompleteChatAsync([new UserChatMessage(prompt)]);
///       return result.Value.Content[0].Text;
///   });
/// </summary>
public class ChatClientAdapter : TracewireAdapter
{
    public ChatClientAdapter(TraceContext ctx, string? model = null) : base(ctx)
    {
        Model = model;
    }

    public string? Model { get; set; }

    public async Task<string> CallAsync(string prompt, Func<string, Task<string>> llmCall)
    {
        OnLlmStart(prompt, Model);

        var sw = Stopwatch.StartNew();
        try
        {
            var response = await llmCall(prompt);
            sw.Stop();
            OnLlmEnd(response, (int)sw.ElapsedMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            sw.Stop();
            OnError(ex);
            throw;
        }
    }

    public async Task<TResult> CallAsync<TResult>(
        string prompt,
        Func<string, Task<TResult>> llmCall,
        Func<TResult, string> extractResponse)
    {
        OnLlmStart(prompt, Model);

        var sw = Stopwatch.StartNew();
        try
        {
            var result = await llmCall(prompt);
            sw.Stop();
            OnLlmEnd(extractResponse(result), (int)sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            OnError(ex);
            throw;
        }
    }
}
