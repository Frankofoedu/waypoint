import type { CreateEventRequest } from "./models.js";
import { WaypointClient } from "./client.js";

export class EventBuffer {
  private buffer: CreateEventRequest[] = [];
  private maxSize: number;
  private flushInterval: number;
  private client: WaypointClient;
  private timer: ReturnType<typeof setInterval> | null = null;

  constructor(client: WaypointClient, maxSize = 1000, flushIntervalMs = 1000) {
    this.client = client;
    this.maxSize = maxSize;
    this.flushInterval = flushIntervalMs;
  }

  enqueue(event: CreateEventRequest): void {
    if (this.buffer.length >= this.maxSize) {
      this.buffer.shift();
    }
    this.buffer.push(event);
  }

  start(): void {
    this.timer = setInterval(() => this.flush(), this.flushInterval);
  }

  async stop(): Promise<void> {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
    await this.flush();
  }

  private async flush(): Promise<void> {
    const events = this.buffer.splice(0, this.buffer.length);
    for (const event of events) {
      try {
        await this.client.createEvent(event);
      } catch (err) {
        console.error("Waypoint: failed to flush event", err);
      }
    }
  }
}
