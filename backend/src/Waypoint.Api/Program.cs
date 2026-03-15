using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Waypoint.Api.Endpoints;
using Waypoint.Api.Middleware;
using Waypoint.Application.DTOs;
using Waypoint.Application.Services;
using Waypoint.Domain.Entities;
using Waypoint.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<WaypointDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<TraceService>();
builder.Services.AddScoped<EventService>();

builder.Services.AddOpenApi();

builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddCors(options =>
    options.AddDefaultPolicy(policy =>
        policy.WithOrigins(builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? ["http://localhost:5173"])
            .AllowAnyHeader()
            .AllowAnyMethod()));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();
app.UseMiddleware<ApiKeyAuthMiddleware>();

app.MapGet("/v1/health", async (WaypointDbContext db) =>
{
    var dbConnected = await db.Database.CanConnectAsync();
    return Results.Ok(new HealthResponse(
        dbConnected ? "healthy" : "degraded",
        dbConnected,
        DateTime.UtcNow));
}).ExcludeFromDescription();

app.MapTraceEndpoints();
app.MapEventEndpoints();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WaypointDbContext>();
    if (db.Database.IsRelational())
        await db.Database.MigrateAsync();
    else
        await db.Database.EnsureCreatedAsync();

    if (app.Environment.IsDevelopment() && !await db.ApiKeys.AnyAsync())
    {
        var org = new Organization { Name = "Demo Org" };
        db.Organizations.Add(org);

        var workspace = new Workspace { Name = "Default", OrganizationId = org.Id };
        db.Workspaces.Add(workspace);

        var rawKey = "wp_dev_testkey_123";
        var apiKey = new ApiKey
        {
            KeyHash = ApiKeyAuthMiddleware.HashKey(rawKey),
            KeyPrefix = "wp_dev",
            WorkspaceId = workspace.Id,
            Scopes = ["read:traces", "write:events", "replay", "admin"]
        };
        db.ApiKeys.Add(apiKey);
        await db.SaveChangesAsync();

        app.Logger.LogInformation("Seeded dev API key: {Key}", rawKey);
    }
}

app.Run();

public partial class Program { }
