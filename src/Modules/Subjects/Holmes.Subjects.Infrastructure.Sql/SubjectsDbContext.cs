using Holmes.Core.Domain.ValueObjects;
using Holmes.Subjects.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Subjects.Infrastructure.Sql;

public class SubjectsDbContext(DbContextOptions<SubjectsDbContext> options)
    : DbContext(options)
{
    public DbSet<SubjectDb> Subjects { get; set; } = null!;

    public DbSet<SubjectAliasDb> SubjectAliases { get; set; } = null!;

    public DbSet<SubjectProjectionDb> SubjectProjections { get; set; } = null!;

    public DbSet<SubjectAddressDb> SubjectAddresses { get; set; } = null!;

    public DbSet<SubjectEmploymentDb> SubjectEmployments { get; set; } = null!;

    public DbSet<SubjectEducationDb> SubjectEducations { get; set; } = null!;

    public DbSet<SubjectReferenceDb> SubjectReferences { get; set; } = null!;

    public DbSet<SubjectPhoneDb> SubjectPhones { get; set; } = null!;

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

            builder.Property(x => x.MiddleName)
                .HasMaxLength(200);

            builder.Property(x => x.DateOfBirth)
                .HasColumnType("date");

            builder.Property(x => x.Email)
                .HasMaxLength(320);

            builder.Property(x => x.EncryptedSsn)
                .HasMaxLength(256);

            builder.Property(x => x.SsnLast4)
                .HasMaxLength(4)
                .IsFixedLength();

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

            builder.HasMany(x => x.Addresses)
                .WithOne(x => x.Subject)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Employments)
                .WithOne(x => x.Subject)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Educations)
                .WithOne(x => x.Subject)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.References)
                .WithOne(x => x.Subject)
                .HasForeignKey(x => x.SubjectId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(x => x.Phones)
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

        modelBuilder.Entity<SubjectProjectionDb>(builder =>
        {
            builder.ToTable("subject_projections");
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

        modelBuilder.Entity<SubjectAddressDb>(builder =>
        {
            builder.ToTable("subject_addresses");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.SubjectId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.Street1)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.Street2)
                .HasMaxLength(256);

            builder.Property(x => x.City)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.State)
                .HasMaxLength(8)
                .IsRequired();

            builder.Property(x => x.PostalCode)
                .HasMaxLength(16)
                .IsRequired();

            builder.Property(x => x.Country)
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(x => x.CountyFips)
                .HasMaxLength(5);

            builder.Property(x => x.FromDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.ToDate)
                .HasColumnType("date");

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasIndex(x => x.SubjectId);
            builder.HasIndex(x => new { x.SubjectId, x.FromDate, x.ToDate });
        });

        modelBuilder.Entity<SubjectEmploymentDb>(builder =>
        {
            builder.ToTable("subject_employments");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.SubjectId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.EmployerName)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.EmployerPhone)
                .HasMaxLength(32);

            builder.Property(x => x.EmployerAddress)
                .HasMaxLength(512);

            builder.Property(x => x.JobTitle)
                .HasMaxLength(128);

            builder.Property(x => x.SupervisorName)
                .HasMaxLength(128);

            builder.Property(x => x.SupervisorPhone)
                .HasMaxLength(32);

            builder.Property(x => x.StartDate)
                .HasColumnType("date")
                .IsRequired();

            builder.Property(x => x.EndDate)
                .HasColumnType("date");

            builder.Property(x => x.ReasonForLeaving)
                .HasMaxLength(256);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasIndex(x => x.SubjectId);
        });

        modelBuilder.Entity<SubjectEducationDb>(builder =>
        {
            builder.ToTable("subject_educations");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.SubjectId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.InstitutionName)
                .HasMaxLength(256)
                .IsRequired();

            builder.Property(x => x.InstitutionAddress)
                .HasMaxLength(512);

            builder.Property(x => x.Degree)
                .HasMaxLength(128);

            builder.Property(x => x.Major)
                .HasMaxLength(128);

            builder.Property(x => x.AttendedFrom)
                .HasColumnType("date");

            builder.Property(x => x.AttendedTo)
                .HasColumnType("date");

            builder.Property(x => x.GraduationDate)
                .HasColumnType("date");

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasIndex(x => x.SubjectId);
        });

        modelBuilder.Entity<SubjectReferenceDb>(builder =>
        {
            builder.ToTable("subject_references");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.SubjectId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.Name)
                .HasMaxLength(128)
                .IsRequired();

            builder.Property(x => x.Phone)
                .HasMaxLength(32);

            builder.Property(x => x.Email)
                .HasMaxLength(320);

            builder.Property(x => x.Relationship)
                .HasMaxLength(64);

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasIndex(x => x.SubjectId);
        });

        modelBuilder.Entity<SubjectPhoneDb>(builder =>
        {
            builder.ToTable("subject_phones");
            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.SubjectId)
                .HasMaxLength(26)
                .IsRequired();

            builder.Property(x => x.PhoneNumber)
                .HasMaxLength(32)
                .IsRequired();

            builder.Property(x => x.CreatedAt)
                .HasColumnType("datetime(6)")
                .HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

            builder.HasIndex(x => x.SubjectId);
        });
    }
}