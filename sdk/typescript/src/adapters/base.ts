import type { TraceContext } from "../trace.js";

export interface WaypointAdapter {
  onLlmStart(prompt: string, metadata?: Record<string, unknown>): void;
  onLlmEnd(response: string, latencyMs?: number, cost?: number): void;
  onToolStart(toolName: string, input?: unknown): void;
  onToolEnd(toolName: string, output?: unknown, latencyMs?: number): void;
  onError(error: Error): void;
}

export function createAdapter(ctx: TraceContext): WaypointAdapter {
  return {
    onLlmStart(prompt, metadata) {
      ctx.logEvent("Prompt", { prompt, ...metadata });
    },
    onLlmEnd(response, latencyMs, cost) {
      ctx.logEvent("ModelResponse", { response }, { latencyMs, cost });
    },
    onToolStart(toolName, input) {
      ctx.logEvent("ToolCall", { tool: toolName, input });
    },
    onToolEnd(toolName, output, latencyMs) {
      ctx.logEvent("ToolCall", { tool: toolName, output, completed: true }, { latencyMs });
    },
    onError(error) {
      ctx.logEvent("Error", { error: error.message, type: error.name });
    },
  };
}
