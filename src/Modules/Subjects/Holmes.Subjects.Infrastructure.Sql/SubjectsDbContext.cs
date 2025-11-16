using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Subjects.Infrastructure.Sql;

public class SubjectsDbContext(DbContextOptions<SubjectsDbContext> options)
    : DbContext(options)
{
    public DbSet<SubjectDb> Subjects { get; set; } = null!;

    public DbSet<SubjectAliasDb> SubjectAliases { get; set; } = null!;

    public DbSet<SubjectDirectoryDb> SubjectDirectory { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasCharSet("utf8mb4");

        modelBuilder.Entity<SubjectDb>(builder =>
        {
            builder.ToTable("subjects");
            builder.HasKey(x => x.SubjectId);

            builder.Property(x => x.SubjectId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.GivenName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.FamilyName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.DateOfBirth)
                .HasColumnType("date");

            builder.Property(x => x.Email)
                .HasMaxLength(320);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.Property(x => x.MergedIntoSubjectId)
                .HasMaxLength(26);

            builder.Property(x => x.MergedBy)
                .HasConversion(
                    v => v.HasValue ? v.Value.ToString() : null,
                    s => string.IsNullOrWhiteSpace(s) ? null : UlidId.Parse(s))
                .HasMaxLength(26);

            builder.Property(x => x.MergedAt)
                .HasColumnType("datetime(6)");

            builder.HasMany(x => x.Aliases)
                .WithOne(x => x.Subject)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<SubjectAliasDb>(builder =>
        {
            builder.ToTable("subject_aliases");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd();

            builder.Property(x => x.SubjectId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.GivenName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.FamilyName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.DateOfBirth)
                .HasColumnType("date");

            builder.HasIndex(x => new { x.SubjectId, x.GivenName, x.FamilyName, x.DateOfBirth })
                .IsUnique();
        });

        modelBuilder.Entity<SubjectDirectoryDb>(builder =>
        {
            builder.ToTable("subject_directory");
            builder.HasKey(x => x.SubjectId);

            builder.Property(x => x.SubjectId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.GivenName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.FamilyName)
                .HasMaxLength(200)
                .IsRequired();

            builder.Property(x => x.DateOfBirth)
                .HasColumnType("date");

            builder.Property(x => x.Email)
                .HasMaxLength(320);

            builder.Property(x => x.IsMerged)
                .HasDefaultValue(false);

            builder.Property(x => x.AliasCount)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");
        });
    }
}
