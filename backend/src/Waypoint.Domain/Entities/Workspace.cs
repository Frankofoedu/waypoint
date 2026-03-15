using Waypoint.Domain.Enums;

namespace Waypoint.Domain.Entities;

public class Workspace
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public required string Name { get; set; }
    public ReplayPolicy ReplayPolicy { get; set; } = ReplayPolicy.Warn;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Organization Organization { get; set; } = null!;
    public ICollection<Trace> Traces { get; set; } = [];
    public ICollection<ApiKey> ApiKeys { get; set; } = [];
}
