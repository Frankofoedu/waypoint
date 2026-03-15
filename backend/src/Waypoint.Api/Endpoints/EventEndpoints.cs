using Waypoint.Api.Middleware;
using Waypoint.Application.DTOs;
using Waypoint.Application.Services;

namespace Waypoint.Api.Endpoints;

public static class EventEndpoints
{
    public static void MapEventEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/v1/events");

        group.MapPost("/", async (CreateEventRequest request, HttpContext ctx, EventService service) =>
        {
            if (!ctx.HasScope("write:events"))
                return Results.Forbid();

            var result = await service.CreateAsync(request, ctx.GetWorkspaceId());
            return result is null
                ? Results.BadRequest(new { error = "Trace not found in this workspace" })
                : Results.Created($"/v1/events/{result.Id}", result);
        });

        group.MapPost("/{eventId:guid}/replay", async (Guid eventId, ReplayRequest request, HttpContext ctx, EventService service) =>
        {
            if (!ctx.HasScope("replay"))
                return Results.Forbid();

            var result = await service.ReplayAsync(eventId, request, ctx.GetWorkspaceId());
            return result is null
                ? Results.BadRequest(new { error = "Event not found or replay blocked by workspace policy" })
                : Results.Ok(result);
        });

        group.MapPost("/{eventId:guid}/pause", async (Guid eventId, PauseRequest request, HttpContext ctx, EventService service) =>
        {
            if (!ctx.HasScope("write:events"))
                return Results.Forbid();

            var success = await service.PauseAsync(eventId, request, ctx.GetWorkspaceId());
            return success ? Results.Ok() : Results.NotFound();
        });

        group.MapPost("/{eventId:guid}/resume", async (Guid eventId, ResumeRequest request, HttpContext ctx, EventService service) =>
        {
            if (!ctx.HasScope("write:events"))
                return Results.Forbid();

            var success = await service.ResumeAsync(eventId, request, ctx.GetWorkspaceId());
            return success ? Results.Ok() : Results.BadRequest(new { error = "Event not found or not paused" });
        });
    }
}
