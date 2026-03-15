import type { TraceContext } from "../trace.js";
import { createAdapter } from "./base.js";

/**
 * Wraps a Vercel AI SDK model to auto-capture all LLM interactions.
 * Works with OpenAI, Anthropic, Google, and any Vercel AI provider.
 *
 * Usage:
 *   import { openai } from "@ai-sdk/openai";
 *   import { generateText } from "ai";
 *   import { wrapLanguageModel } from "tracewire-sdk/adapters/ai-sdk";
 *
 *   const model = wrapLanguageModel(openai("gpt-4o"), traceContext);
 *   const { text } = await generateText({ model, prompt: "Hello!" });
 */
export function wrapLanguageModel(model: unknown, ctx: TraceContext): unknown {
  const adapter = createAdapter(ctx);
  const original = model as Record<string, unknown>;

  return new Proxy(original, {
    get(target, prop, receiver) {
      const value = Reflect.get(target, prop, receiver);

      if (prop === "doGenerate" && typeof value === "function") {
        return async (...args: unknown[]) => {
          const params = args[0] as Record<string, unknown> | undefined;
          const prompt = extractPrompt(params);
          adapter.onLlmStart(prompt, { model: (target as Record<string, unknown>).modelId });

          const start = Date.now();
          try {
            const result = await (value as Function).apply(target, args);
            const latencyMs = Date.now() - start;
            const response = extractResponse(result as Record<string, unknown>);
            adapter.onLlmEnd(response, latencyMs);
            return result;
          } catch (err) {
            adapter.onError(err as Error);
            throw err;
          }
        };
      }

      if (prop === "doStream" && typeof value === "function") {
        return async (...args: unknown[]) => {
          const params = args[0] as Record<string, unknown> | undefined;
          const prompt = extractPrompt(params);
          adapter.onLlmStart(prompt, { model: (target as Record<string, unknown>).modelId, streaming: true });

          const start = Date.now();
          try {
            const result = await (value as Function).apply(target, args);
            const latencyMs = Date.now() - start;
            adapter.onLlmEnd("[streaming]", latencyMs);
            return result;
          } catch (err) {
            adapter.onError(err as Error);
            throw err;
          }
        };
      }

      return value;
    },
  });
}

function extractPrompt(params: Record<string, unknown> | undefined): string {
  if (!params) return "";
  if (typeof params.prompt === "string") return params.prompt;
  if (Array.isArray(params.messages)) {
    const last = params.messages[params.messages.length - 1] as Record<string, unknown> | undefined;
    if (last && typeof last.content === "string") return last.content;
  }
  return JSON.stringify(params.prompt ?? params.messages ?? "");
}

function extractResponse(result: Record<string, unknown>): string {
  if (typeof result.text === "string") return result.text;
  if (typeof result.content === "string") return result.content;
  return JSON.stringify(result);
}
