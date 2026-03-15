using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Waypoint.Api.Endpoints;
using Waypoint.Api.Middleware;
using Waypoint.Application.DTOs;
using Waypoint.Application.Services;
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
}

app.Run();

public partial class Program { }
