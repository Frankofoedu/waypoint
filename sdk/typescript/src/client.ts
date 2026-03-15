import type {
  CreateEventRequest,
  CreateTraceRequest,
  EventResponse,
  TraceDetailResponse,
  TraceResponse,
} from "./models.js";

export class WaypointClient {
  private baseUrl: string;
  private apiKey: string;

  constructor(baseUrl = "http://localhost:5185", apiKey = "") {
    this.baseUrl = baseUrl.replace(/\/$/, "");
    this.apiKey = apiKey;
  }

  private async request<T>(path: string, options: RequestInit = {}): Promise<T> {
    const resp = await fetch(`${this.baseUrl}${path}`, {
      ...options,
      headers: {
        "Content-Type": "application/json",
        "X-API-Key": this.apiKey,
        ...options.headers,
      },
    });
    if (!resp.ok) {
      throw new Error(`Waypoint API error: ${resp.status} ${resp.statusText}`);
    }
    return resp.json() as Promise<T>;
  }

  async createTrace(agentName: string, metadata?: Record<string, unknown>): Promise<TraceResponse> {
    const body: CreateTraceRequest = {
      agentName,
      metadata: metadata ? JSON.stringify(metadata) : undefined,
    };
    return this.request<TraceResponse>("/v1/traces", {
      method: "POST",
      body: JSON.stringify(body),
    });
  }

  async createEvent(request: CreateEventRequest): Promise<EventResponse> {
    return this.request<EventResponse>("/v1/events", {
      method: "POST",
      body: JSON.stringify(request),
    });
  }

  async getTrace(traceId: string): Promise<TraceDetailResponse> {
    return this.request<TraceDetailResponse>(`/v1/traces/${traceId}`);
  }

  async pauseEvent(eventId: string, timeoutSeconds = 60): Promise<void> {
    await this.request(`/v1/events/${eventId}/pause`, {
      method: "POST",
      body: JSON.stringify({ timeoutSeconds }),
    });
  }

  async resumeEvent(eventId: string, decision: string, comments?: string): Promise<void> {
    await this.request(`/v1/events/${eventId}/resume`, {
      method: "POST",
      body: JSON.stringify({ decision, comments }),
    });
  }
}
