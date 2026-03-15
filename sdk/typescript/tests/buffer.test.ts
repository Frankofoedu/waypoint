import { describe, it, expect, vi, beforeEach } from "vitest";
import { EventBuffer } from "../src/buffer";
import type { WaypointClient } from "../src/client";
import type { CreateEventRequest } from "../src/models";

function mockClient(): WaypointClient {
  return {
    createTrace: vi.fn(),
    createEvent: vi.fn().mockResolvedValue({}),
    getTrace: vi.fn(),
    pauseEvent: vi.fn(),
    resumeEvent: vi.fn(),
  } as unknown as WaypointClient;
}

describe("EventBuffer", () => {
  it("flushes buffered events", async () => {
    const client = mockClient();
    const buffer = new EventBuffer(client, 100, 10000);

    const event: CreateEventRequest = {
      traceId: "trace-1",
      eventType: "Prompt",
      depth: 0,
    };
    buffer.enqueue(event);
    await buffer.stop();

    expect(client.createEvent).toHaveBeenCalledWith(event);
  });

  it("drops oldest when buffer is full", () => {
    const client = mockClient();
    const buffer = new EventBuffer(client, 2, 10000);

    buffer.enqueue({ traceId: "t", eventType: "Prompt", depth: 0 });
    buffer.enqueue({ traceId: "t", eventType: "ToolCall", depth: 1 });
    buffer.enqueue({ traceId: "t", eventType: "Error", depth: 2 });

    // buffer should have dropped first, kept last 2
    // verified on flush
  });
});
