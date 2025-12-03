using Holmes.Notifications.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Notifications.Infrastructure.Sql;

public class NotificationsDbContext(DbContextOptions<NotificationsDbContext> options)
    : DbContext(options)
{
    public DbSet<NotificationRequestDb> NotificationRequests => Set<NotificationRequestDb>();
    public DbSet<DeliveryAttemptDb> DeliveryAttempts => Set<DeliveryAttemptDb>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasDefaultSchema("notifications");

        modelBuilder.Entity<NotificationRequestDb>(entity =>
        {
            entity.ToTable("notification_requests");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).HasMaxLength(26).IsRequired();
            entity.Property(e => e.CustomerId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.OrderId).HasMaxLength(26);
            entity.Property(e => e.SubjectId).HasMaxLength(26);
            entity.Property(e => e.RecipientAddress).HasMaxLength(512).IsRequired();
            entity.Property(e => e.RecipientDisplayName).HasMaxLength(256);
            entity.Property(e => e.RecipientMetadataJson).HasColumnType("JSON");
            entity.Property(e => e.ContentSubject).HasMaxLength(512).IsRequired();
            entity.Property(e => e.ContentBody).HasColumnType("TEXT");
            entity.Property(e => e.ContentTemplateId).HasMaxLength(128);
            entity.Property(e => e.ContentTemplateDataJson).HasColumnType("JSON");
            entity.Property(e => e.ScheduleJson).HasColumnType("JSON");
            entity.Property(e => e.CorrelationId).HasMaxLength(64);
            entity.Property(e => e.CreatedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.ScheduledFor).HasColumnType("datetime(6)");
            entity.Property(e => e.ProcessedAt).HasColumnType("datetime(6)");
            entity.Property(e => e.DeliveredAt).HasColumnType("datetime(6)");

            entity.HasIndex(e => e.CustomerId);
            entity.HasIndex(e => e.OrderId);
            entity.HasIndex(e => e.Status);
            entity.HasIndex(e => new { e.Status, e.CreatedAt });
            entity.HasIndex(e => new { e.Status, e.ScheduledFor });
            entity.HasIndex(e => e.CorrelationId);
        });

        modelBuilder.Entity<DeliveryAttemptDb>(entity =>
        {
            entity.ToTable("delivery_attempts");

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedOnAdd();
            entity.Property(e => e.NotificationRequestId).HasMaxLength(26).IsRequired();
            entity.Property(e => e.ProviderMessageId).HasMaxLength(256);
            entity.Property(e => e.FailureReason).HasMaxLength(1024);
            entity.Property(e => e.AttemptedAt).HasColumnType("datetime(6)");

            entity.HasOne(e => e.NotificationRequest)
                .WithMany(n => n.DeliveryAttempts)
                .HasForeignKey(e => e.NotificationRequestId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(e => e.NotificationRequestId);
            entity.HasIndex(e => e.AttemptedAt);
        });
    }
}