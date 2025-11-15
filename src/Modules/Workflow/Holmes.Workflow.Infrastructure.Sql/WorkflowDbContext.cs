using Holmes.Workflow.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Holmes.Workflow.Infrastructure.Sql;

public class WorkflowDbContext(DbContextOptions<WorkflowDbContext> options)
    : DbContext(options)
{
    public DbSet<OrderDb> Orders => Set<OrderDb>();
    public DbSet<OrderSummaryDb> OrderSummaries => Set<OrderSummaryDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureOrders(modelBuilder.Entity<OrderDb>());
        ConfigureOrderSummaries(modelBuilder.Entity<OrderSummaryDb>());
    }

    private static void ConfigureOrders(EntityTypeBuilder<OrderDb> builder)
    {
        builder.ToTable("workflow_orders");
        builder.HasKey(x => x.OrderId);
        builder.Property(x => x.OrderId)
            .HasMaxLength(26)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.SubjectId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(x => x.CustomerId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(x => x.PolicySnapshotId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.PackageCode)
            .HasMaxLength(64);

        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.BlockedFromStatus)
            .HasMaxLength(32);

        builder.Property(x => x.LastStatusReason)
            .HasMaxLength(256);

        builder.Property(x => x.ActiveIntakeSessionId)
            .HasMaxLength(26);

        builder.Property(x => x.LastCompletedIntakeSessionId)
            .HasMaxLength(26);

        builder.HasIndex(x => x.SubjectId);
        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
    }

    private static void ConfigureOrderSummaries(EntityTypeBuilder<OrderSummaryDb> builder)
    {
        builder.ToTable("order_summary");
        builder.HasKey(x => x.OrderId);
        builder.Property(x => x.OrderId)
            .HasMaxLength(26)
            .ValueGeneratedNever();
        builder.Property(x => x.SubjectId)
            .HasMaxLength(26)
            .IsRequired();
        builder.Property(x => x.CustomerId)
            .HasMaxLength(26)
            .IsRequired();
        builder.Property(x => x.PolicySnapshotId)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(x => x.PackageCode)
            .HasMaxLength(64);
        builder.Property(x => x.Status)
            .HasMaxLength(32)
            .IsRequired();
        builder.Property(x => x.LastStatusReason)
            .HasMaxLength(256);

        builder.HasIndex(x => x.CustomerId);
        builder.HasIndex(x => x.Status);
    }
}