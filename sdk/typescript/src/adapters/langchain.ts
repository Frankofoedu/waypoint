import type { TraceContext } from "../trace.js";
import { createAdapter, type WaypointAdapter } from "./base.js";

export function createLangChainCallback(ctx: TraceContext) {
  const adapter = createAdapter(ctx);

  return {
    handleLLMStart(_llm: unknown, prompts: string[]) {
      for (const prompt of prompts) {
        adapter.onLlmStart(prompt);
      }
    },
    handleLLMEnd(output: unknown) {
      adapter.onLlmEnd(String(output));
    },
    handleLLMError(err: Error) {
      adapter.onError(err);
    },
    handleToolStart(_tool: unknown, input: string) {
      adapter.onToolStart("unknown", input);
    },
    handleToolEnd(output: string) {
      adapter.onToolEnd("unknown", output);
    },
    handleToolError(err: Error) {
      adapter.onError(err);
    },
  };
}
