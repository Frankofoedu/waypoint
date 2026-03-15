import type { TraceDetail, TracesListResponse, TraceStatus, ReplayResponse } from "../types";

const BASE = "/v1";
const API_KEY = localStorage.getItem("waypoint_api_key") ?? "";

async function request<T>(path: string, options: RequestInit = {}): Promise<T> {
  const resp = await fetch(`${BASE}${path}`, {
    ...options,
    headers: {
      "Content-Type": "application/json",
      "X-API-Key": API_KEY,
      ...options.headers,
    },
  });
  if (!resp.ok) throw new Error(`API error: ${resp.status}`);
  return resp.json();
}

export function listTraces(params?: {
  status?: TraceStatus;
  agentName?: string;
  cursor?: string;
  limit?: number;
}): Promise<TracesListResponse> {
  const qs = new URLSearchParams();
  if (params?.status) qs.set("status", params.status);
  if (params?.agentName) qs.set("agentName", params.agentName);
  if (params?.cursor) qs.set("cursor", params.cursor);
  if (params?.limit) qs.set("limit", String(params.limit));
  return request(`/traces?${qs}`);
}

export function getTrace(traceId: string): Promise<TraceDetail> {
  return request(`/traces/${traceId}`);
}

export function replayEvent(eventId: string, modifiedPayload?: string, newBranchName?: string): Promise<ReplayResponse> {
  return request(`/events/${eventId}/replay`, {
    method: "POST",
    body: JSON.stringify({ modifiedPayload, newBranchName }),
  });
}

export function pauseEvent(eventId: string, timeoutSeconds = 60): Promise<void> {
  return request(`/events/${eventId}/pause`, {
    method: "POST",
    body: JSON.stringify({ timeoutSeconds }),
  });
}

export function resumeEvent(eventId: string, decision: string, comments?: string): Promise<void> {
  return request(`/events/${eventId}/resume`, {
    method: "POST",
    body: JSON.stringify({ decision, comments }),
  });
}
