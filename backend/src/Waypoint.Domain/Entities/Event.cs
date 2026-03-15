using Waypoint.Domain.Enums;

namespace Waypoint.Domain.Entities;

public class Event
{
    public Guid Id { get; set; }
    public Guid TraceId { get; set; }
    public Guid? ParentId { get; set; }
    public string BranchName { get; set; } = "main";
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public EventType EventType { get; set; }
    public string? Payload { get; set; }
    public int? LatencyMs { get; set; }
    public decimal? Cost { get; set; }
    public int StepOrder { get; set; }
    public int Depth { get; set; }
    public string? StateSnapshot { get; set; }
    public string? SideEffects { get; set; }
    public HitlStatus HitlStatus { get; set; } = HitlStatus.None;
    public int? HitlTimeoutSeconds { get; set; }
    public string? HitlDecision { get; set; }
    public bool IsDeleted { get; set; }

    public Trace Trace { get; set; } = null!;
    public Event? Parent { get; set; }
    public ICollection<Event> Children { get; set; } = [];
}
