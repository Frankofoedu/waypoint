using Waypoint.Domain.Enums;

namespace Waypoint.Domain.Entities;

public class Trace
{
    public Guid Id { get; set; }
    public required string AgentName { get; set; }
    public TraceStatus Status { get; set; } = TraceStatus.Running;
    public DateTime StartTime { get; set; } = DateTime.UtcNow;
    public DateTime? EndTime { get; set; }
    public Guid? UserId { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid WorkspaceId { get; set; }
    public string? Metadata { get; set; }
    public byte? TraceFlags { get; set; }
    public string? TraceState { get; set; }

    public Workspace Workspace { get; set; } = null!;
    public ICollection<Event> Events { get; set; } = [];
}
