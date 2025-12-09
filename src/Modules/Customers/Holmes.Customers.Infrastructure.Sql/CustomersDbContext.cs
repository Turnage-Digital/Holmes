using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Infrastructure.Sql;

public class CustomersDbContext(DbContextOptions<CustomersDbContext> options)
    : DbContext(options)
{
    public DbSet<CustomerDb> Customers { get; set; } = null!;

    public DbSet<CustomerAdminDb> CustomerAdmins { get; set; } = null!;

    public DbSet<CustomerProjectionDb> CustomerProjections { get; set; } = null!;

    public DbSet<CustomerProfileDb> CustomerProfiles { get; set; } = null!;

    public DbSet<CustomerContactDb> CustomerContacts { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<CustomerDb>(builder =>
        {
            builder.ToTable("customers");
            builder.HasKey(x => x.CustomerId);

            builder.Property(x => x.CustomerId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasMany(x => x.Admins)
                .WithOne(x => x.Customer)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CustomerAdminDb>(builder =>
        {
            builder.ToTable("customer_admins");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.CustomerId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.UserId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.AssignedBy)
                .HasConversion(v => v.ToString(), s => UlidId.Parse(s))
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.AssignedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasIndex(x => new { x.CustomerId, x.UserId })
                .IsUnique();
        });

        modelBuilder.Entity<CustomerProjectionDb>(builder =>
        {
            builder.ToTable("customer_projections");
            builder.HasKey(x => x.CustomerId);

            builder.Property(x => x.CustomerId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.Property(x => x.AdminCount)
                .IsRequired();
        });

        modelBuilder.Entity<CustomerProfileDb>(builder =>
        {
            builder.ToTable("customer_profiles");
            builder.HasKey(x => x.CustomerId);

            builder.Property(x => x.CustomerId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.TenantId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.PolicySnapshotId)
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.BillingEmail)
                .HasMaxLength(320);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.Property(x => x.UpdatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        });

        modelBuilder.Entity<CustomerContactDb>(builder =>
        {
            builder.ToTable("customer_contacts");
            builder.HasKey(x => x.ContactId);

            builder.Property(x => x.ContactId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.CustomerId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(320)
                .IsRequired();

            builder.Property(x => x.Phone)
                .HasMaxLength(64);

            builder.Property(x => x.Role)
                .HasMaxLength(128);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasOne(x => x.Customer)
                .WithMany(x => x.Contacts)
                .HasForeignKey(x => x.CustomerId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}