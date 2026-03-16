import type { TraceContext } from "../trace.js";
import { createAdapter } from "./base.js";

export function createLangChainCallback(ctx: TraceContext) {
  const adapter = createAdapter(ctx);
  let llmStartTime: number | undefined;
  const toolNames = new Map<string, string>();
  const toolStartTimes = new Map<string, number>();

  return {
    handleLLMStart(_llm: unknown, prompts: string[]) {
      llmStartTime = Date.now();
      for (const prompt of prompts) {
        adapter.onLlmStart(prompt);
      }
    },
    handleLLMEnd(output: unknown) {
      const latencyMs = llmStartTime !== undefined ? Date.now() - llmStartTime : undefined;
      llmStartTime = undefined;
      adapter.onLlmEnd(String(output), latencyMs);
    },
    handleLLMError(err: Error) {
      llmStartTime = undefined;
      adapter.onError(err);
    },
    handleToolStart(tool: Record<string, unknown>, input: string, runId?: string) {
      const toolName = (tool?.name as string) ?? "unknown";
      if (runId) {
        toolNames.set(runId, toolName);
        toolStartTimes.set(runId, Date.now());
      }
      adapter.onToolStart(toolName, input);
    },
    handleToolEnd(output: string, runId?: string) {
      const toolName = runId ? (toolNames.get(runId) ?? "unknown") : "unknown";
      const latencyMs = runId && toolStartTimes.has(runId) ? Date.now() - toolStartTimes.get(runId)! : undefined;
      if (runId) {
        toolNames.delete(runId);
        toolStartTimes.delete(runId);
      }
      adapter.onToolEnd(toolName, output, latencyMs);
    },
    handleToolError(err: Error, runId?: string) {
      if (runId) {
        toolNames.delete(runId);
        toolStartTimes.delete(runId);
      }
      adapter.onError(err);
    },
  };
}
