import type { CreateEventRequest, EventType } from "./models.js";
import { WaypointClient } from "./client.js";
import { EventBuffer } from "./buffer.js";

export class TraceContext {
  private client: WaypointClient;
  private buffer: EventBuffer;
  readonly traceId: string;
  private snapshot: boolean;
  private lastEventId: string | undefined;
  private depth = 0;

  constructor(client: WaypointClient, buffer: EventBuffer, traceId: string, snapshot = false) {
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

    const deadline = Date.now() + timeout * 1000;
    while (Date.now() < deadline) {
      const traceData = await this.client.getTrace(this.traceId);
      const event = traceData.events?.find((e) => e.id === eventId);
      if (event?.hitlStatus === "Resumed") {
        if (event.hitlDecision) {
          const decision = JSON.parse(event.hitlDecision);
          return decision.decision ?? "approve";
        }
        return "approve";
      }
      await new Promise((r) => setTimeout(r, 2000));
    }

    return fallback;
  }
}

export async function trace<T>(
  agentName: string,
  fn: (ctx: TraceContext) => Promise<T>,
  options?: { baseUrl?: string; apiKey?: string; snapshot?: boolean },
): Promise<T> {
  const client = new WaypointClient(options?.baseUrl, options?.apiKey);
  const buffer = new EventBuffer(client);
  buffer.start();

  try {
    const traceResp = await client.createTrace(agentName);
    const ctx = new TraceContext(client, buffer, traceResp.id, options?.snapshot);
    return await fn(ctx);
  } finally {
    await buffer.stop();
  }
}
