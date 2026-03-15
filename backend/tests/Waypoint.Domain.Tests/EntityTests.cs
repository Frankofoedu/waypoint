using Waypoint.Domain.Entities;
using Waypoint.Domain.Enums;

namespace Waypoint.Domain.Tests;

public class EntityTests
{
    [Fact]
    public void Trace_DefaultStatus_IsRunning()
    {
        var trace = new Trace { AgentName = "test" };
        Assert.Equal(TraceStatus.Running, trace.Status);
    }

    [Fact]
    public void Event_DefaultBranch_IsMain()
    {
        var evt = new Event();
        Assert.Equal("main", evt.BranchName);
    }

    [Fact]
    public void Event_DefaultHitlStatus_IsNone()
    {
        var evt = new Event();
        Assert.Equal(HitlStatus.None, evt.HitlStatus);
    }

    [Fact]
    public void Event_DefaultIsDeleted_IsFalse()
    {
        var evt = new Event();
        Assert.False(evt.IsDeleted);
    }

    [Fact]
    public void Workspace_DefaultReplayPolicy_IsWarn()
    {
        var ws = new Workspace { Name = "test" };
        Assert.Equal(ReplayPolicy.Warn, ws.ReplayPolicy);
    }
}
