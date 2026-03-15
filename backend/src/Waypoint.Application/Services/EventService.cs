using Microsoft.EntityFrameworkCore;
using Waypoint.Application.DTOs;
using Waypoint.Domain.Entities;
using Waypoint.Domain.Enums;
using Waypoint.Infrastructure;

namespace Waypoint.Application.Services;

public class EventService(WaypointDbContext db)
{
    public async Task<EventResponse?> CreateAsync(CreateEventRequest request, Guid workspaceId)
    {
        var trace = await db.Traces
            .FirstOrDefaultAsync(t => t.Id == request.TraceId && t.WorkspaceId == workspaceId);
        if (trace is null) return null;

        var evt = new Event
        {
            TraceId = request.TraceId,
            ParentId = request.ParentId,
            BranchName = request.BranchName ?? "main",
            EventType = request.EventType,
            Payload = request.Payload,
            LatencyMs = request.LatencyMs,
            Cost = request.Cost,
            Depth = request.Depth,
            StateSnapshot = request.StateSnapshot,
            SideEffects = request.SideEffects
        };

        db.Events.Add(evt);
        await db.SaveChangesAsync();
        return MapToResponse(evt);
    }

    public async Task<ReplayResponse?> ReplayAsync(Guid eventId, ReplayRequest request, Guid workspaceId)
    {
        var sourceEvent = await db.Events
            .Include(e => e.Trace)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Trace.WorkspaceId == workspaceId);
        if (sourceEvent is null) return null;

        var workspace = await db.Workspaces.FindAsync(workspaceId);
        var warnings = await GetAncestorSideEffects(sourceEvent);

        if (workspace?.ReplayPolicy == ReplayPolicy.Block && warnings.Count > 0)
            return null;

        var branchName = request.NewBranchName ?? $"replay_{DateTime.UtcNow:yyyyMMdd_HHmmss}";
        var newEvent = new Event
        {
            TraceId = sourceEvent.TraceId,
            ParentId = sourceEvent.ParentId,
            BranchName = branchName,
            EventType = sourceEvent.EventType,
            Payload = request.ModifiedPayload ?? sourceEvent.Payload,
            Depth = sourceEvent.Depth,
            StateSnapshot = sourceEvent.StateSnapshot
        };

        db.Events.Add(newEvent);
        await db.SaveChangesAsync();

        return new ReplayResponse(newEvent.Id, branchName, warnings);
    }

    public async Task<bool> PauseAsync(Guid eventId, PauseRequest request, Guid workspaceId)
    {
        var evt = await db.Events
            .Include(e => e.Trace)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Trace.WorkspaceId == workspaceId);
        if (evt is null) return false;

        evt.HitlStatus = HitlStatus.Paused;
        evt.HitlTimeoutSeconds = request.TimeoutSeconds;
        await db.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ResumeAsync(Guid eventId, ResumeRequest request, Guid workspaceId)
    {
        var evt = await db.Events
            .Include(e => e.Trace)
            .FirstOrDefaultAsync(e => e.Id == eventId && e.Trace.WorkspaceId == workspaceId);
        if (evt is null || evt.HitlStatus != HitlStatus.Paused) return false;

        evt.HitlStatus = HitlStatus.Resumed;
        evt.HitlDecision = System.Text.Json.JsonSerializer.Serialize(new
        {
            decision = request.Decision,
            comments = request.Comments,
            resumed_at = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return true;
    }

    private async Task<List<SideEffectWarning>> GetAncestorSideEffects(Event evt)
    {
        var warnings = new List<SideEffectWarning>();
        var current = evt;

        while (current?.ParentId != null)
        {
            current = await db.Events.FindAsync(current.ParentId);
            if (current?.SideEffects != null)
            {
                warnings.Add(new SideEffectWarning(
                    current.Id, current.StepOrder, current.SideEffects, current.Timestamp));
            }
        }

        return warnings;
    }

    internal static EventResponse MapToResponse(Event e) => new(
        e.Id, e.TraceId, e.ParentId, e.BranchName, e.Timestamp, e.EventType,
        e.Payload, e.LatencyMs, e.Cost, e.StepOrder, e.Depth,
        e.StateSnapshot, e.SideEffects, e.HitlStatus, e.HitlDecision);
}
