using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Waypoint.Infrastructure;

namespace Waypoint.Api.Middleware;

public class ApiKeyAuthMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, WaypointDbContext db)
    {
        var path = context.Request.Path.Value;
        if (path != null && path.StartsWith("/v1/health", StringComparison.OrdinalIgnoreCase))
        {
            await next(context);
            return;
        }

        var apiKey = context.Request.Headers["X-API-Key"].FirstOrDefault()
            ?? context.Request.Headers.Authorization.FirstOrDefault()?.Replace("Bearer ", "");

        if (string.IsNullOrEmpty(apiKey))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "API key required" });
            return;
        }

        var keyHash = HashKey(apiKey);
        var key = await db.ApiKeys
            .Include(k => k.Workspace)
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash && !k.IsRevoked);

        if (key is null || (key.ExpiresAt.HasValue && key.ExpiresAt < DateTime.UtcNow))
        {
            context.Response.StatusCode = 401;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid or expired API key" });
            return;
        }

        context.Items["WorkspaceId"] = key.WorkspaceId;
        context.Items["OrganizationId"] = key.Workspace.OrganizationId;
        context.Items["Scopes"] = key.Scopes;

        await next(context);
    }

    public static string HashKey(string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(key));
        return Convert.ToHexStringLower(hash);
    }
}

public static class HttpContextExtensions
{
    public static Guid GetWorkspaceId(this HttpContext context)
        => (Guid)context.Items["WorkspaceId"]!;

    public static Guid GetOrganizationId(this HttpContext context)
        => (Guid)context.Items["OrganizationId"]!;

    public static string[] GetScopes(this HttpContext context)
        => (string[])context.Items["Scopes"]!;

    public static bool HasScope(this HttpContext context, string scope)
        => context.GetScopes().Contains(scope) || context.GetScopes().Contains("admin");
}
