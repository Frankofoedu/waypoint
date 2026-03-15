using Waypoint.Api.Middleware;
using Waypoint.Application.DTOs;
using Waypoint.Application.Services;
using Waypoint.Domain.Enums;

namespace Waypoint.Api.Endpoints;

public static class TraceEndpoints
{
    public static void MapTraceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/traces");

        group.MapPost("/", async (CreateTraceRequest request, HttpContext ctx, TraceService service) =>
        {
            if (!ctx.HasScope("write:events"))
                return Results.Forbid();

            var result = await service.CreateAsync(request, ctx.GetWorkspaceId(), ctx.GetOrganizationId());
            return Results.Created($"/v1/traces/{result.Id}", result);
        });

        group.MapGet("/", async (
            HttpContext ctx,
            TraceService service,
            TraceStatus? status,
            string? agentName,
            DateTime? from,
            DateTime? to,
            string? cursor,
            int? limit) =>
        {
            if (!ctx.HasScope("read:traces"))
                return Results.Forbid();

            var result = await service.ListAsync(
                ctx.GetWorkspaceId(), status, agentName, from, to, cursor, limit ?? 50);
            return Results.Ok(result);
        });

        group.MapGet("/{traceId:guid}", async (Guid traceId, HttpContext ctx, TraceService service) =>
        {
            if (!ctx.HasScope("read:traces"))
                return Results.Forbid();

            var result = await service.GetByIdAsync(traceId, ctx.GetWorkspaceId());
            return result is null ? Results.NotFound() : Results.Ok(result);
        });
    }
}
