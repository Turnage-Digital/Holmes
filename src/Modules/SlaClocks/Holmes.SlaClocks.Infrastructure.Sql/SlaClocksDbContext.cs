using Holmes.SlaClocks.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.SlaClocks.Infrastructure.Sql;

public class SlaClocksDbContext(DbContextOptions<SlaClocksDbContext> options)
    : DbContext(options)
{
    public DbSet<SlaClockDb> SlaClocks => Set<SlaClockDb>();
    public DbSet<HolidayDb> Holidays => Set<HolidayDb>();
    public DbSet<SlaClockProjectionDb> SlaClockProjections => Set<SlaClockProjectionDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<SlaClockDb>(entity =>
        {
            entity.ToTable("sla_clocks");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(26).IsRequired();
            entity.Property(e => e.OrderId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.CustomerId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.PauseReason).HasMaxLength(256);
            entity.Property(e => e.StartedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.DeadlineAt).HasColumnType("datetime(6)");
            entity.Property(e => e.AtRiskThresholdAt).HasColumnType("datetime(6)");
            entity.Property(e => e.AtRiskAt).HasColumnType("datetime(6)");
            entity.Property(e => e.BreachedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.PausedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.AtRiskThresholdPercent).HasColumnType("decimal(3,2)");

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => new { e.State, e.AtRiskThresholdAt });
            entity.HasIndex(e => new { e.State, e.DeadlineAt });
            entity.HasIndex(e => new { e.CustomerId, e.State });
        });

        modelBuilder.Entity<HolidayDb>(entity =>
        {
            entity.ToTable("holidays");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.CustomerId).HasMaxLength(26);
            entity.Property(e => e.Date).HasColumnType("date");
            entity.Property(e => e.Name).HasMaxLength(128).IsRequired();

            entity.HasIndex(e => new { e.CustomerId, e.Date });
        });

        modelBuilder.Entity<SlaClockProjectionDb>(entity =>
        {
            entity.ToTable("sla_clock_projections");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(26).IsRequired();
            entity.Property(e => e.OrderId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.CustomerId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.PauseReason).HasMaxLength(256);
            entity.Property(e => e.StartedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.DeadlineAt).HasColumnType("datetime(6)");
            entity.Property(e => e.AtRiskThresholdAt).HasColumnType("datetime(6)");
            entity.Property(e => e.AtRiskAt).HasColumnType("datetime(6)");
            entity.Property(e => e.BreachedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.PausedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.CompletedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.AtRiskThresholdPercent).HasColumnType("decimal(3,2)");

            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => new { e.State, e.AtRiskThresholdAt });
            entity.HasIndex(e => new { e.State, e.DeadlineAt });
            entity.HasIndex(e => new { e.CustomerId, e.State });
        });
    }
}
