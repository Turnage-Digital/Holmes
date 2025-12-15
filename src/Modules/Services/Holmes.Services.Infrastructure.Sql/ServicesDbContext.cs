using Holmes.Services.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Services.Infrastructure.Sql;

public class ServicesDbContext : DbContext
{
    public ServicesDbContext(DbContextOptions<ServicesDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceRequestDb> ServiceRequests => Set<ServiceRequestDb>();
    public DbSet<ServiceResultDb> ServiceResults => Set<ServiceResultDb>();
    public DbSet<ServiceCatalogSnapshotDb> ServiceCatalogSnapshots => Set<ServiceCatalogSnapshotDb>();
    public DbSet<ServiceProjectionDb> ServiceProjections => Set<ServiceProjectionDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceRequestDb>(entity =>
        {
            entity.ToTable("service_requests");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasMaxLength(26).IsFixedLength();
            entity.Property(e => e.OrderId).HasMaxLength(26).IsFixedLength().IsRequired();
            entity.Property(e => e.CustomerId).HasMaxLength(26).IsFixedLength().IsRequired();
            entity.Property(e => e.CatalogSnapshotId).HasMaxLength(26).IsFixedLength();

            entity.Property(e => e.ServiceTypeCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Tier).IsRequired();

            entity.Property(e => e.ScopeValue).HasMaxLength(128);

            entity.Property(e => e.Status).IsRequired();

            entity.Property(e => e.VendorCode).HasMaxLength(32);
            entity.Property(e => e.VendorReferenceId).HasMaxLength(256);

            entity.Property(e => e.LastError).HasMaxLength(1024);

            entity.Property(e => e.CreatedAt).HasPrecision(6).IsRequired();
            entity.Property(e => e.UpdatedAt).HasPrecision(6).IsRequired();
            entity.Property(e => e.DispatchedAt).HasPrecision(6);
            entity.Property(e => e.CompletedAt).HasPrecision(6);
            entity.Property(e => e.FailedAt).HasPrecision(6);
            entity.Property(e => e.CanceledAt).HasPrecision(6);

            entity.HasIndex(e => e.OrderId).HasDatabaseName("idx_order");
            entity.HasIndex(e => new { e.CustomerId, e.Status }).HasDatabaseName("idx_customer_status");
            entity.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("idx_status_created");
            entity.HasIndex(e => new { e.VendorCode, e.VendorReferenceId }).HasDatabaseName("idx_vendor_ref");
            entity.HasIndex(e => new { e.OrderId, e.Tier }).HasDatabaseName("idx_order_tier");

            entity.HasOne(e => e.Result)
                .WithOne(r => r.ServiceRequest)
                .HasForeignKey<ServiceResultDb>(r => r.ServiceRequestId);
        });

        modelBuilder.Entity<ServiceResultDb>(entity =>
        {
            entity.ToTable("service_results");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasMaxLength(26).IsFixedLength();
            entity.Property(e => e.ServiceRequestId).HasMaxLength(26).IsFixedLength().IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.RecordsJson).HasColumnType("json");
            entity.Property(e => e.RawResponseHash).HasMaxLength(64);
            entity.Property(e => e.VendorReferenceId).HasMaxLength(256);
            entity.Property(e => e.ReceivedAt).HasPrecision(6).IsRequired();
            entity.Property(e => e.NormalizedAt).HasPrecision(6);

            entity.HasIndex(e => e.ServiceRequestId).HasDatabaseName("idx_service_request");
        });

        modelBuilder.Entity<ServiceCatalogSnapshotDb>(entity =>
        {
            entity.ToTable("service_catalog_snapshots");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasMaxLength(26).IsFixedLength();
            entity.Property(e => e.CustomerId).HasMaxLength(26).IsFixedLength().IsRequired();
            entity.Property(e => e.Version).IsRequired();
            entity.Property(e => e.ConfigJson).HasColumnType("json").IsRequired();
            entity.Property(e => e.CreatedAt).HasPrecision(6).IsRequired();
            entity.Property(e => e.CreatedBy).HasMaxLength(26).IsFixedLength();

            entity.HasIndex(e => new { e.CustomerId, e.Version })
                .IsUnique()
                .HasDatabaseName("idx_customer_version");
            entity.HasIndex(e => e.CustomerId).HasDatabaseName("idx_customer");
        });

        modelBuilder.Entity<ServiceProjectionDb>(entity =>
        {
            entity.ToTable("service_projections");
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Id).HasMaxLength(26).IsFixedLength();
            entity.Property(e => e.OrderId).HasMaxLength(26).IsFixedLength().IsRequired();
            entity.Property(e => e.CustomerId).HasMaxLength(26).IsFixedLength().IsRequired();
            entity.Property(e => e.ServiceTypeCode).HasMaxLength(64).IsRequired();
            entity.Property(e => e.Category).IsRequired();
            entity.Property(e => e.Status).IsRequired();
            entity.Property(e => e.Tier).IsRequired();
            entity.Property(e => e.ScopeType).HasMaxLength(32);
            entity.Property(e => e.ScopeValue).HasMaxLength(128);
            entity.Property(e => e.VendorCode).HasMaxLength(32);
            entity.Property(e => e.VendorReferenceId).HasMaxLength(256);
            entity.Property(e => e.LastError).HasMaxLength(1024);
            entity.Property(e => e.CancelReason).HasMaxLength(256);
            entity.Property(e => e.AttemptCount).IsRequired();
            entity.Property(e => e.RecordCount).IsRequired();
            entity.Property(e => e.CreatedAt).HasPrecision(6).IsRequired();
            entity.Property(e => e.DispatchedAt).HasPrecision(6);
            entity.Property(e => e.CompletedAt).HasPrecision(6);
            entity.Property(e => e.FailedAt).HasPrecision(6);
            entity.Property(e => e.CanceledAt).HasPrecision(6);
            entity.Property(e => e.UpdatedAt).HasPrecision(6).IsRequired();

            entity.HasIndex(e => e.OrderId).HasDatabaseName("idx_proj_order");
            entity.HasIndex(e => new { e.CustomerId, e.Status }).HasDatabaseName("idx_proj_customer_status");
            entity.HasIndex(e => new { e.Status, e.CreatedAt }).HasDatabaseName("idx_proj_status_created");
        });
    }
}