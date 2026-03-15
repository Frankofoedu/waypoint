namespace Waypoint.Domain.Entities;

public class ApiKey
{
    public Guid Id { get; set; }
    public required string KeyHash { get; set; }
    public required string KeyPrefix { get; set; }
    public Guid WorkspaceId { get; set; }
    public required string[] Scopes { get; set; }
    public bool IsRevoked { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }

    public Workspace Workspace { get; set; } = null!;
}
