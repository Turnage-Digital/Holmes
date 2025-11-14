using Holmes.Intake.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Holmes.Intake.Infrastructure.Sql;

public class IntakeDbContext(DbContextOptions<IntakeDbContext> options)
    : DbContext(options)
{
    public DbSet<ConsentArtifactDb> ConsentArtifacts => Set<ConsentArtifactDb>();
    public DbSet<IntakeSessionDb> IntakeSessions => Set<IntakeSessionDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        ConfigureConsentArtifacts(modelBuilder.Entity<ConsentArtifactDb>());
        ConfigureIntakeSessions(modelBuilder.Entity<IntakeSessionDb>());
        // Phase 2 will configure aggregates (IntakeSession, Order workflow projections, etc.).
    }

    private static void ConfigureConsentArtifacts(EntityTypeBuilder<ConsentArtifactDb> builder)
    {
        builder.ToTable("consent_artifacts");
        builder.HasKey(x => x.ArtifactId);
        builder.Property(x => x.ArtifactId)
            .HasMaxLength(26)
            .IsRequired()
            .ValueGeneratedNever();

        builder.Property(x => x.OrderId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(x => x.SubjectId)
            .HasMaxLength(26)
            .IsRequired();

        builder.Property(x => x.MimeType)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(x => x.Hash)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(x => x.HashAlgorithm)
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(x => x.SchemaVersion)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(x => x.MetadataJson)
            .HasColumnType("json")
            .HasDefaultValue("{}");

        builder.Property(x => x.Payload)
            .IsRequired();

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.SubjectId);
    }

    private static void ConfigureIntakeSessions(EntityTypeBuilder<IntakeSessionDb> builder)
    {
        builder.ToTable("intake_sessions");
        builder.HasKey(x => x.IntakeSessionId);
        builder.Property(x => x.IntakeSessionId)
            .HasMaxLength(26)
            .ValueGeneratedNever();
        builder.Property(x => x.OrderId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.SubjectId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.CustomerId).HasMaxLength(26).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResumeToken).HasMaxLength(128).IsRequired();
        builder.Property(x => x.PolicySnapshotJson).HasColumnType("json").IsRequired();
        builder.Property(x => x.AnswersPayloadCipherText).HasColumnType("longtext");
        builder.Property(x => x.CancellationReason).HasMaxLength(256);
        builder.Property(x => x.SupersededBySessionId).HasMaxLength(26);
        builder.Property(x => x.ConsentArtifactId).HasMaxLength(26);
        builder.Property(x => x.ConsentMimeType).HasMaxLength(256);
        builder.Property(x => x.ConsentHash).HasMaxLength(128);
        builder.Property(x => x.ConsentHashAlgorithm).HasMaxLength(32);
        builder.Property(x => x.ConsentSchemaVersion).HasMaxLength(64);
        builder.Property(x => x.AnswersSchemaVersion).HasMaxLength(64);
        builder.Property(x => x.AnswersPayloadHash).HasMaxLength(128);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.SubjectId);
        builder.HasIndex(x => x.CustomerId);
    }
}