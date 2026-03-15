import { describe, it, expect, vi } from "vitest";
import { wrapLanguageModel } from "../src/adapters/ai-sdk";
import { TraceContext } from "../src/trace";
import { EventBuffer } from "../src/buffer";
import type { TracewireClient } from "../src/client";

function mockClient(): TracewireClient {
  return {
    createTrace: vi.fn(),
    createEvent: vi.fn().mockResolvedValue({}),
    getTrace: vi.fn(),
    pauseEvent: vi.fn(),
    resumeEvent: vi.fn(),
  } as unknown as TracewireClient;
}

describe("wrapLanguageModel", () => {
  it("intercepts doGenerate and logs events", async () => {
    const client = mockClient();
    const buffer = new EventBuffer(client, 100, 10000);
    const ctx = new TraceContext(client, buffer, "trace-1");

    const logSpy = vi.spyOn(ctx, "logEvent");

    const fakeModel = {
      modelId: "gpt-4o",
      doGenerate: vi.fn().mockResolvedValue({ text: "Hello back!" }),
    };

    const wrapped = wrapLanguageModel(fakeModel, ctx) as typeof fakeModel;
    const result = await wrapped.doGenerate({ prompt: "Hello!" });

    expect(result.text).toBe("Hello back!");
    expect(fakeModel.doGenerate).toHaveBeenCalledTimes(1);
    expect(logSpy).toHaveBeenCalledTimes(2);
    expect(logSpy.mock.calls[0][0]).toBe("Prompt");
    expect(logSpy.mock.calls[1][0]).toBe("ModelResponse");
  });

  it("logs errors on doGenerate failure", async () => {
    const client = mockClient();
    const buffer = new EventBuffer(client, 100, 10000);
    const ctx = new TraceContext(client, buffer, "trace-1");

    const logSpy = vi.spyOn(ctx, "logEvent");

    const fakeModel = {
      modelId: "gpt-4o",
      doGenerate: vi.fn().mockRejectedValue(new Error("Rate limited")),
    };

    const wrapped = wrapLanguageModel(fakeModel, ctx) as typeof fakeModel;
    await expect(wrapped.doGenerate({ prompt: "Hello!" })).rejects.toThrow("Rate limited");

    expect(logSpy).toHaveBeenCalledTimes(2);
    expect(logSpy.mock.calls[0][0]).toBe("Prompt");
    expect(logSpy.mock.calls[1][0]).toBe("Error");
  });

  it("passes through non-intercepted properties", () => {
    const client = mockClient();
    const buffer = new EventBuffer(client, 100, 10000);
    const ctx = new TraceContext(client, buffer, "trace-1");

    const fakeModel = { modelId: "gpt-4o", provider: "openai" };
    const wrapped = wrapLanguageModel(fakeModel, ctx) as typeof fakeModel;

    expect(wrapped.modelId).toBe("gpt-4o");
    expect(wrapped.provider).toBe("openai");
  });
});
