using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql;

public class UsersDbContext(DbContextOptions<UsersDbContext> options)
    : DbContext(options)
{
    public DbSet<UserDb> Users { get; set; } = null!;

    public DbSet<UserExternalIdentityDb> UserExternalIdentities { get; set; } = null!;

    public DbSet<UserProjectionDb> UserProjections { get; set; } = null!;

    public DbSet<UserRoleMembershipDb> UserRoleMemberships { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<UserDb>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(x => x.UserId);

            builder.Property(x => x.UserId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.Email)
                .HasMaxLength(320)
                .IsRequired();

            builder.Property(x => x.DisplayName)
                .HasMaxLength(256);

            builder.Property(x => x.Status)
                .HasConversion<string>()
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasMany(x => x.ExternalIdentities)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.RoleMemberships)
                .WithOne(x => x.User)
                .HasForeignKey(x => x.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserExternalIdentityDb>(builder =>
        {
            builder.ToTable("user_external_identities");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.UserId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.Issuer)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.Subject)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.AuthenticationMethod)
                .HasMaxLength(100);

            builder.Property(x => x.LinkedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.Property(x => x.LastSeenAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasIndex(x => new { x.UserId, x.Issuer, x.Subject })
                .IsUnique();
        });

        modelBuilder.Entity<UserProjectionDb>(builder =>
        {
            builder.ToTable("user_projections");
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

            builder.HasIndex(x => new { x.Issuer, x.Subject })
                .IsUnique();
        });

        modelBuilder.Entity<UserRoleMembershipDb>(builder =>
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