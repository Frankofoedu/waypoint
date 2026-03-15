export type TraceStatus = "Running" | "Success" | "Error";

export type EventType =
  | "Prompt"
  | "ToolCall"
  | "ModelResponse"
  | "MemoryWrite"
  | "Error"
  | "Retry";

export type HitlStatus = "None" | "Paused" | "Resumed" | "TimedOut";

export interface CreateTraceRequest {
  agentName: string;
  metadata?: string;
}

export interface CreateEventRequest {
  traceId: string;
  parentId?: string;
  branchName?: string;
  eventType: EventType;
  payload?: string;
  latencyMs?: number;
  cost?: number;
  depth: number;
  stateSnapshot?: string;
  sideEffects?: string;
}

export interface TraceResponse {
  id: string;
  agentName: string;
  status: TraceStatus;
  startTime: string;
  endTime?: string;
  organizationId: string;
  workspaceId: string;
  metadata?: string;
}

export interface EventResponse {
  id: string;
  traceId: string;
  parentId?: string;
  branchName: string;
  timestamp: string;
  eventType: EventType;
  payload?: string;
  latencyMs?: number;
  cost?: number;
  stepOrder: number;
  depth: number;
  stateSnapshot?: string;
  sideEffects?: string;
  hitlStatus: HitlStatus;
  hitlDecision?: string;
}

export interface TraceDetailResponse extends TraceResponse {
  events: EventResponse[];
}
