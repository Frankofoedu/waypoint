using System.Text.Json.Serialization;

namespace Waypoint.Sdk;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum TraceStatus { Running, Success, Error }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum EventType { Prompt, ToolCall, ModelResponse, MemoryWrite, Error, Retry }

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum HitlStatus { None, Paused, Resumed, TimedOut }

public record CreateTraceRequest(
    [property: JsonPropertyName("agentName")] string AgentName,
    [property: JsonPropertyName("metadata")] string? Metadata = null);

public record CreateEventRequest(
    [property: JsonPropertyName("traceId")] Guid TraceId,
    [property: JsonPropertyName("eventType")] EventType EventType,
    [property: JsonPropertyName("depth")] int Depth,
    [property: JsonPropertyName("parentId")] Guid? ParentId = null,
    [property: JsonPropertyName("branchName")] string? BranchName = null,
    [property: JsonPropertyName("payload")] string? Payload = null,
    [property: JsonPropertyName("latencyMs")] int? LatencyMs = null,
    [property: JsonPropertyName("cost")] decimal? Cost = null,
    [property: JsonPropertyName("stateSnapshot")] string? StateSnapshot = null,
    [property: JsonPropertyName("sideEffects")] string? SideEffects = null);

public record TraceResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("agentName")] string AgentName,
    [property: JsonPropertyName("status")] TraceStatus Status,
    [property: JsonPropertyName("startTime")] DateTime StartTime,
    [property: JsonPropertyName("endTime")] DateTime? EndTime,
    [property: JsonPropertyName("organizationId")] Guid OrganizationId,
    [property: JsonPropertyName("workspaceId")] Guid WorkspaceId,
    [property: JsonPropertyName("metadata")] string? Metadata);

public record EventResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("traceId")] Guid TraceId,
    [property: JsonPropertyName("parentId")] Guid? ParentId,
    [property: JsonPropertyName("branchName")] string BranchName,
    [property: JsonPropertyName("timestamp")] DateTime Timestamp,
    [property: JsonPropertyName("eventType")] EventType EventType,
    [property: JsonPropertyName("payload")] string? Payload,
    [property: JsonPropertyName("latencyMs")] int? LatencyMs,
    [property: JsonPropertyName("cost")] decimal? Cost,
    [property: JsonPropertyName("stepOrder")] int StepOrder,
    [property: JsonPropertyName("depth")] int Depth,
    [property: JsonPropertyName("stateSnapshot")] string? StateSnapshot,
    [property: JsonPropertyName("sideEffects")] string? SideEffects,
    [property: JsonPropertyName("hitlStatus")] HitlStatus HitlStatus,
    [property: JsonPropertyName("hitlDecision")] string? HitlDecision);

public record TraceDetailResponse(
    [property: JsonPropertyName("id")] Guid Id,
    [property: JsonPropertyName("agentName")] string AgentName,
    [property: JsonPropertyName("status")] TraceStatus Status,
    [property: JsonPropertyName("startTime")] DateTime StartTime,
    [property: JsonPropertyName("endTime")] DateTime? EndTime,
    [property: JsonPropertyName("organizationId")] Guid OrganizationId,
    [property: JsonPropertyName("workspaceId")] Guid WorkspaceId,
    [property: JsonPropertyName("metadata")] string? Metadata,
    [property: JsonPropertyName("events")] List<EventResponse> Events);
