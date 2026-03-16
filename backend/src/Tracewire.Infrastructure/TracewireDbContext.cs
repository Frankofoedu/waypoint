using Microsoft.EntityFrameworkCore;
using Tracewire.Domain.Entities;
using Tracewire.Domain.Enums;

namespace Tracewire.Infrastructure;

public class TracewireDbContext(DbContextOptions<TracewireDbContext> options) : DbContext(options)
{
    public DbSet<Organization> Organizations => Set<Organization>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<ApiKey> ApiKeys => Set<ApiKey>();
    public DbSet<Trace> Traces => Set<Trace>();
    public DbSet<Event> Events => Set<Event>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var isPostgres = Database.ProviderName == "Npgsql.EntityFrameworkCore.PostgreSQL";

        if (isPostgres)
        {
            modelBuilder.HasPostgresEnum<TraceStatus>();
            modelBuilder.HasPostgresEnum<EventType>();
            modelBuilder.HasPostgresEnum<HitlStatus>();
            modelBuilder.HasPostgresEnum<ReplayPolicy>();
        }

        modelBuilder.Entity<Organization>(e =>
        {
            e.HasKey(o => o.Id);
        });

        modelBuilder.Entity<Workspace>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasOne(w => w.Organization)
                .WithMany(o => o.Workspaces)
                .HasForeignKey(w => w.OrganizationId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<ApiKey>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.KeyHash).IsUnique();
            e.HasOne(a => a.Workspace)
                .WithMany(w => w.ApiKeys)
                .HasForeignKey(a => a.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Trace>(e =>
        {
            e.HasKey(t => t.Id);
            e.HasIndex(t => t.WorkspaceId);
            e.HasIndex(t => t.OrganizationId);
            e.HasOne(t => t.Workspace)
                .WithMany(w => w.Traces)
                .HasForeignKey(t => t.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            if (isPostgres)
                e.Property(t => t.Metadata).HasColumnType("jsonb");
        });

        modelBuilder.Entity<Event>(e =>
        {
            e.HasKey(ev => ev.Id);
            e.HasQueryFilter(ev => !ev.IsDeleted);

            e.HasIndex(ev => ev.TraceId);
            e.HasIndex(ev => ev.ParentId);
            e.HasIndex(ev => new { ev.TraceId, ev.BranchName, ev.Depth, ev.StepOrder });

            e.HasOne(ev => ev.Trace)
                .WithMany(t => t.Events)
                .HasForeignKey(ev => ev.TraceId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(ev => ev.Parent)
                .WithMany(ev => ev.Children)
                .HasForeignKey(ev => ev.ParentId)
                .OnDelete(DeleteBehavior.Restrict);

            if (isPostgres)
            {
                e.Property(ev => ev.StepOrder).UseIdentityAlwaysColumn();
                e.Property(ev => ev.Payload).HasColumnType("jsonb");
                e.Property(ev => ev.StateSnapshot).HasColumnType("jsonb");
                e.Property(ev => ev.SideEffects).HasColumnType("jsonb");
                e.Property(ev => ev.HitlDecision).HasColumnType("jsonb");
                e.ToTable(t => t.HasCheckConstraint(
                    "CK_Event_PayloadSize",
                    "octet_length(\"Payload\"::text) <= 102400"));
                e.ToTable(t => t.HasCheckConstraint(
                    "CK_Event_StateSnapshotSize",
                    "octet_length(\"StateSnapshot\"::text) <= 524288"));
            }
        });
    }
}
