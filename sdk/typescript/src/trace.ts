import type { CreateEventRequest, EventType } from "./models.js";
import { TracewireClient } from "./client.js";
import { EventBuffer } from "./buffer.js";

export type ReplayCallback = (branchName: string, payload: string | undefined, eventId: string) => Promise<void>;

export class TraceContext {
  private client: TracewireClient;
  private buffer: EventBuffer;
  readonly traceId: string;
  private snapshot: boolean;
  private lastEventId: string | undefined;
  private depth = 0;
  private replayAbort: AbortController | null = null;

  constructor(client: TracewireClient, buffer: EventBuffer, traceId: string, snapshot = false) {
    this.client = client;
    this.buffer = buffer;
    this.traceId = traceId;
    this.snapshot = snapshot;
  }

  logEvent(
    eventType: EventType,
    payload?: Record<string, unknown>,
    options?: { latencyMs?: number; cost?: number; stateSnapshot?: Record<string, unknown>; sideEffects?: Record<string, unknown>[] },
  ): void {
    const event: CreateEventRequest = {
      traceId: this.traceId,
      parentId: this.lastEventId,
      eventType,
      payload: payload ? JSON.stringify(payload) : undefined,
      latencyMs: options?.latencyMs,
      cost: options?.cost,
      depth: this.depth,
      stateSnapshot: options?.stateSnapshot ? JSON.stringify(options.stateSnapshot) : undefined,
      sideEffects: options?.sideEffects ? JSON.stringify(options.sideEffects) : undefined,
    };
    this.buffer.enqueue(event);
    this.depth++;
  }

  registerSideEffect(effectType: string, details?: Record<string, unknown>): void {
    this.logEvent("ToolCall", { type: "side_effect", effectType }, {
      sideEffects: [{ type: effectType, details: details ?? {} }],
    });
  }

  async pauseForHuman(timeout = 60, fallback: "abort" | "continue" | "escalate" = "abort"): Promise<string> {
    if (!this.lastEventId) {
      this.logEvent("Prompt", { type: "hitl_pause" });
      await this.buffer.stop();
      this.buffer.start();
      await new Promise((r) => setTimeout(r, 500));
    }

    const eventId = this.lastEventId;
    if (!eventId) return fallback;

    await this.client.pauseEvent(eventId, timeout);

    const controller = new AbortController();
    const timer = setTimeout(() => controller.abort(), timeout * 1000);

    try {
      return await this.waitViaSse(eventId, controller.signal);
    } catch {
      return fallback;
    } finally {
      clearTimeout(timer);
    }
  }

  private async waitViaSse(eventId: string, signal: AbortSignal): Promise<string> {
    const url = `${this.client.baseUrl}/v1/traces/${this.traceId}/stream?apiKey=${encodeURIComponent(this.client.apiKey)}`;
    const resp = await fetch(url, { signal });
    if (!resp.ok || !resp.body) throw new Error("SSE connection failed");

    const reader = resp.body.getReader();
    const decoder = new TextDecoder();
    let buffer = "";

    try {
      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });

        while (buffer.includes("\n\n")) {
          const idx = buffer.indexOf("\n\n");
          const message = buffer.slice(0, idx);
          buffer = buffer.slice(idx + 2);

          for (const line of message.split("\n")) {
            if (line.startsWith("data: ")) {
              const data = JSON.parse(line.slice(6));
              if (data.eventId === eventId && data.status === "Resumed") {
                if (data.decision) {
                  const decision = JSON.parse(data.decision);
                  return decision.decision ?? "approve";
                }
                return "approve";
              }
            }
          }
        }
      }
    } finally {
      reader.releaseLock();
    }
    throw new Error("SSE stream ended without resume");
  }

  onReplay(callback: ReplayCallback): void {
    this.replayAbort = new AbortController();
    this.listenForReplays(callback, this.replayAbort.signal);
  }

  private async listenForReplays(callback: ReplayCallback, signal: AbortSignal): Promise<void> {
    const url = `${this.client.baseUrl}/v1/traces/${this.traceId}/stream?apiKey=${encodeURIComponent(this.client.apiKey)}`;
    try {
      const resp = await fetch(url, { signal });
      if (!resp.ok || !resp.body) return;

      const reader = resp.body.getReader();
      const decoder = new TextDecoder();
      let buffer = "";

      while (true) {
        const { done, value } = await reader.read();
        if (done) break;
        buffer += decoder.decode(value, { stream: true });

        while (buffer.includes("\n\n")) {
          const idx = buffer.indexOf("\n\n");
          const message = buffer.slice(0, idx);
          buffer = buffer.slice(idx + 2);

          for (const line of message.split("\n")) {
            if (!line.startsWith("data: ")) continue;
            const data = JSON.parse(line.slice(6));
            if (data.status === "Replay") {
              await callback(data.branchName ?? "", data.payload, data.eventId ?? "");
            }
          }
        }
      }
    } catch {
      // listener stopped
    }
  }

  stopReplayListener(): void {
    this.replayAbort?.abort();
    this.replayAbort = null;
  }
}

export async function trace<T>(
  agentName: string,
  fn: (ctx: TraceContext) => Promise<T>,
  options?: { baseUrl?: string; apiKey?: string; snapshot?: boolean },
): Promise<T> {
  const client = new TracewireClient(options?.baseUrl, options?.apiKey);
  const buffer = new EventBuffer(client);
  buffer.start();

  try {
    const traceResp = await client.createTrace(agentName);
    const ctx = new TraceContext(client, buffer, traceResp.id, options?.snapshot);
    try {
      return await fn(ctx);
    } finally {
      ctx.stopReplayListener();
    }
  } finally {
    await buffer.stop();
  }
}
