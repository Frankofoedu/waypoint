namespace Waypoint.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Workspace> Workspaces { get; set; } = [];
}
