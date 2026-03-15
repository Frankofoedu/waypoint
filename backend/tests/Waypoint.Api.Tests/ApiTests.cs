using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Waypoint.Api.Middleware;
using Waypoint.Application.DTOs;
using Waypoint.Domain.Entities;
using Waypoint.Domain.Enums;
using Waypoint.Infrastructure;

namespace Waypoint.Api.Tests;

public class ApiTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };
    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;
    private const string TestApiKey = "test-key-12345";
    private static readonly string _dbName = $"WaypointTests_{Guid.NewGuid()}";

    public async Task InitializeAsync()
    {
        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptors = services
                        .Where(d => d.ServiceType.FullName?.Contains("EntityFrameworkCore") == true
                                 || d.ServiceType == typeof(DbContextOptions<WaypointDbContext>))
                        .ToList();
                    foreach (var d in descriptors) services.Remove(d);

                    services.AddDbContext<WaypointDbContext>(options =>
                        options.UseInMemoryDatabase(_dbName));
                });
            });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<WaypointDbContext>();
        await SeedTestData(db);

        _client = _factory.CreateClient();
        _client.DefaultRequestHeaders.Add("X-API-Key", TestApiKey);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    private static async Task SeedTestData(WaypointDbContext db)
    {
        var org = new Organization { Id = Guid.NewGuid(), Name = "Test Org" };
        db.Organizations.Add(org);

        var workspace = new Workspace { Id = Guid.NewGuid(), Name = "Test Workspace", OrganizationId = org.Id };
        db.Workspaces.Add(workspace);

        var keyHash = ApiKeyAuthMiddleware.HashKey(TestApiKey);
        var apiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            KeyHash = keyHash,
            KeyPrefix = "test",
            WorkspaceId = workspace.Id,
            Scopes = ["read:traces", "write:events", "replay", "admin"]
        };
        db.ApiKeys.Add(apiKey);

        await db.SaveChangesAsync();
    }

    [Fact]
    public async Task Health_ReturnsOk()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/v1/health");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Unauthorized_WithoutApiKey()
    {
        var client = _factory.CreateClient();
        var response = await client.GetAsync("/v1/traces");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTrace_ReturnsCreated()
    {
        var response = await _client.PostAsJsonAsync("/v1/traces", new { agentName = "test_agent" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        var trace = await response.Content.ReadFromJsonAsync<TraceResponse>(JsonOpts);
        Assert.NotNull(trace);
        Assert.Equal("test_agent", trace.AgentName);
        Assert.Equal(TraceStatus.Running, trace.Status);
    }

    [Fact]
    public async Task ListTraces_ReturnsTraces()
    {
        await _client.PostAsJsonAsync("/v1/traces", new { agentName = "list_test" });
        var response = await _client.GetFromJsonAsync<TracesListResponse>("/v1/traces", JsonOpts);

        Assert.NotNull(response);
        Assert.True(response.Items.Count > 0);
    }

    [Fact]
    public async Task CreateEvent_ForTrace()
    {
        var traceResp = await _client.PostAsJsonAsync("/v1/traces", new { agentName = "event_test" });
        var trace = await traceResp.Content.ReadFromJsonAsync<TraceResponse>(JsonOpts);

        var eventResp = await _client.PostAsJsonAsync("/v1/events", new
        {
            traceId = trace!.Id,
            eventType = "Prompt",
            depth = 0,
            payload = "{\"prompt\": \"hello\"}"
        });
        Assert.Equal(HttpStatusCode.Created, eventResp.StatusCode);
    }

    [Fact]
    public async Task GetTraceDetail_IncludesEvents()
    {
        var traceResp = await _client.PostAsJsonAsync("/v1/traces", new { agentName = "detail_test" });
        var trace = await traceResp.Content.ReadFromJsonAsync<TraceResponse>(JsonOpts);

        await _client.PostAsJsonAsync("/v1/events", new
        {
            traceId = trace!.Id,
            eventType = "Prompt",
            depth = 0
        });

        var detail = await _client.GetFromJsonAsync<TraceDetailResponse>($"/v1/traces/{trace.Id}", JsonOpts);
        Assert.NotNull(detail);
        Assert.NotEmpty(detail.Events);
    }

    [Fact]
    public async Task PauseAndResume_HitlFlow()
    {
        var traceResp = await _client.PostAsJsonAsync("/v1/traces", new { agentName = "hitl_test" });
        var trace = await traceResp.Content.ReadFromJsonAsync<TraceResponse>(JsonOpts);

        var eventResp = await _client.PostAsJsonAsync("/v1/events", new
        {
            traceId = trace!.Id,
            eventType = "Prompt",
            depth = 0
        });
        var evt = await eventResp.Content.ReadFromJsonAsync<EventResponse>(JsonOpts);

        var pauseResp = await _client.PostAsJsonAsync($"/v1/events/{evt!.Id}/pause", new { timeoutSeconds = 60 });
        Assert.Equal(HttpStatusCode.OK, pauseResp.StatusCode);

        var resumeResp = await _client.PostAsJsonAsync($"/v1/events/{evt.Id}/resume", new
        {
            decision = "approve",
            comments = "looks good"
        });
        Assert.Equal(HttpStatusCode.OK, resumeResp.StatusCode);
    }
}
