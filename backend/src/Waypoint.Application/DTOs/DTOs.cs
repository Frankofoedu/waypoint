using System.Text.Json.Serialization;
using Waypoint.Domain.Enums;

namespace Waypoint.Application.DTOs;

public record CreateTraceRequest(
    string AgentName,
    string? Metadata = null);

public record TraceResponse(
    Guid Id,
    string AgentName,
    TraceStatus Status,
    DateTime StartTime,
    DateTime? EndTime,
    Guid? UserId,
    Guid OrganizationId,
    Guid WorkspaceId,
    string? Metadata,
    byte? TraceFlags,
    string? TraceState);

public record TraceDetailResponse(
    Guid Id,
    string AgentName,
    TraceStatus Status,
    DateTime StartTime,
    DateTime? EndTime,
    Guid? UserId,
    Guid OrganizationId,
    Guid WorkspaceId,
    string? Metadata,
    byte? TraceFlags,
    string? TraceState,
    List<EventResponse> Events);

public record TracesListResponse(
    List<TraceResponse> Items,
    string? NextCursor,
    int TotalCount);

public record CreateEventRequest(
    Guid TraceId,
    Guid? ParentId,
    string? BranchName,
    EventType EventType,
    string? Payload,
    int? LatencyMs,
    decimal? Cost,
    int Depth,
    string? StateSnapshot,
    string? SideEffects);

public record EventResponse(
    Guid Id,
    Guid TraceId,
    Guid? ParentId,
    string BranchName,
    DateTime Timestamp,
    EventType EventType,
    string? Payload,
    int? LatencyMs,
    decimal? Cost,
    int StepOrder,
    int Depth,
    string? StateSnapshot,
    string? SideEffects,
    HitlStatus HitlStatus,
    string? HitlDecision);

public record ReplayRequest(
    string? ModifiedPayload = null,
    string? NewBranchName = null);

public record ReplayResponse(
    Guid NewEventId,
    string BranchName,
    List<SideEffectWarning> Warnings);

public record SideEffectWarning(
    Guid EventId,
    int StepOrder,
    string SideEffects,
    DateTime Timestamp);

public record PauseRequest(
    int? TimeoutSeconds = 60);

public record ResumeRequest(
    string Decision,
    string? Comments = null);

public record HealthResponse(
    string Status,
    bool DatabaseConnected,
    DateTime Timestamp);
