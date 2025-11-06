using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql;

public class UsersDbContext(DbContextOptions<UsersDbContext> options) : DbContext(options)
{
    public DbSet<UserDirectoryEntry> UserDirectory { get; set; } = null!;

    public DbSet<UserRoleMembership> UserRoleMemberships { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<UserDirectoryEntry>(builder =>
        {
            builder.ToTable("user_directory");
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.UserId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(320)
                .IsRequired();

            builder.Property(x => x.DisplayName)
                .HasMaxLength(256);

            builder.Property(x => x.Issuer)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Subject)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.LastAuthenticatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();
        });

        modelBuilder.Entity<UserRoleMembership>(builder =>
        {
            builder.ToTable("user_role_memberships");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.UserId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.Role)
                .HasConversion<string>()
                .HasMaxLength(64)
                .IsRequired();

            builder.Property(x => x.CustomerId)
                .HasMaxLength(26);

            builder.Property(x => x.GrantedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.Property(x => x.GrantedBy)
                .HasConversion(v => v.ToString(), s => UlidId.Parse(s))
                .HasMaxLength(26)
                .IsRequired();

            builder.HasIndex(x => new { x.UserId, x.Role, x.CustomerId })
                .IsUnique();
        });
    }
}
