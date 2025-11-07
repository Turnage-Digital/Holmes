using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Customers.Infrastructure.Sql;

public class CustomersDbContext(DbContextOptions<CustomersDbContext> options)
    : DbContext(options)
{
    public DbSet<CustomerDb> Customers { get; set; } = null!;

    public DbSet<CustomerAdminDb> CustomerAdmins { get; set; } = null!;

    public DbSet<CustomerDirectoryDb> CustomerDirectory { get; set; } = null!;

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

        modelBuilder.Entity<CustomerDirectoryDb>(builder =>
        {
            builder.ToTable("customer_directory");
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
    }
}