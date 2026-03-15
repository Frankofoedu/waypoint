using Microsoft.EntityFrameworkCore;
using Waypoint.Application.DTOs;
using Waypoint.Domain.Entities;
using Waypoint.Domain.Enums;
using Waypoint.Infrastructure;

namespace Waypoint.Application.Services;

public class TraceService(WaypointDbContext db)
{
    public async Task<TraceResponse> CreateAsync(CreateTraceRequest request, Guid workspaceId, Guid organizationId)
    {
        var trace = new Trace
        {
            AgentName = request.AgentName,
            OrganizationId = organizationId,
            WorkspaceId = workspaceId,
            Metadata = request.Metadata
        };

        db.Traces.Add(trace);
        await db.SaveChangesAsync();
        return MapToResponse(trace);
    }

    public async Task<TracesListResponse> ListAsync(Guid workspaceId, TraceStatus? status, string? agentName, DateTime? from, DateTime? to, string? cursor, int limit = 50)
    {
        var query = db.Traces
            .Where(t => t.WorkspaceId == workspaceId)
            .AsQueryable();

        if (status.HasValue)
            query = query.Where(t => t.Status == status.Value);
        if (!string.IsNullOrEmpty(agentName))
            query = query.Where(t => t.AgentName == agentName);
        if (from.HasValue)
            query = query.Where(t => t.StartTime >= from.Value);
        if (to.HasValue)
            query = query.Where(t => t.StartTime <= to.Value);

        if (!string.IsNullOrEmpty(cursor) && Guid.TryParse(cursor, out var cursorId))
            query = query.Where(t => t.Id.CompareTo(cursorId) > 0);

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(t => t.Id)
            .Take(limit + 1)
            .ToListAsync();

        string? nextCursor = null;
        if (items.Count > limit)
        {
            nextCursor = items[limit].Id.ToString();
            items = items.Take(limit).ToList();
        }

        return new TracesListResponse(
            items.Select(MapToResponse).ToList(),
            nextCursor,
            totalCount);
    }

    public async Task<TraceDetailResponse?> GetByIdAsync(Guid traceId, Guid workspaceId)
    {
        var trace = await db.Traces
            .Where(t => t.Id == traceId && t.WorkspaceId == workspaceId)
            .FirstOrDefaultAsync();

        if (trace is null) return null;

        var events = await db.Events
            .Where(e => e.TraceId == traceId)
            .OrderBy(e => e.BranchName)
            .ThenBy(e => e.Depth)
            .ThenBy(e => e.StepOrder)
            .ToListAsync();

        return new TraceDetailResponse(
            trace.Id, trace.AgentName, trace.Status, trace.StartTime, trace.EndTime,
            trace.UserId, trace.OrganizationId, trace.WorkspaceId, trace.Metadata,
            trace.TraceFlags, trace.TraceState,
            events.Select(EventService.MapToResponse).ToList());
    }

    private static TraceResponse MapToResponse(Trace t) => new(
        t.Id, t.AgentName, t.Status, t.StartTime, t.EndTime,
        t.UserId, t.OrganizationId, t.WorkspaceId, t.Metadata,
        t.TraceFlags, t.TraceState);
}
