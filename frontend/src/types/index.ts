export type TraceStatus = "Running" | "Success" | "Error";
export type EventType = "Prompt" | "ToolCall" | "ModelResponse" | "MemoryWrite" | "Error" | "Retry";
export type HitlStatus = "None" | "Paused" | "Resumed" | "TimedOut";

export interface Trace {
  id: string;
  agentName: string;
  status: TraceStatus;
  startTime: string;
  endTime?: string;
  organizationId: string;
  workspaceId: string;
  metadata?: string;
}

export interface TraceEvent {
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

export interface TraceDetail extends Trace {
  events: TraceEvent[];
}

export interface TracesListResponse {
  items: Trace[];
  nextCursor?: string;
  totalCount: number;
}

export interface ReplayResponse {
  newEventId: string;
  branchName: string;
  warnings: SideEffectWarning[];
}

export interface SideEffectWarning {
  eventId: string;
  stepOrder: number;
  sideEffects: string;
  timestamp: string;
}
