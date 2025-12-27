using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.ValueObjects;
using Holmes.Notifications.Infrastructure.Sql.Entities;

namespace Holmes.Notifications.Infrastructure.Sql.Mappers;

public static class NotificationMapper
{
    public static Notification ToDomain(NotificationDb db)
    {
        var recipientMetadata = string.IsNullOrEmpty(db.RecipientMetadataJson)
            ? new Dictionary<string, string>()
            : JsonSerializer.Deserialize<Dictionary<string, string>>(db.RecipientMetadataJson)
              ?? new Dictionary<string, string>();

        var templateData = string.IsNullOrEmpty(db.ContentTemplateDataJson)
            ? new Dictionary<string, object>()
            : JsonSerializer.Deserialize<Dictionary<string, object>>(db.ContentTemplateDataJson)
              ?? new Dictionary<string, object>();

        var schedule = string.IsNullOrEmpty(db.ScheduleJson)
            ? NotificationSchedule.Immediate()
            : JsonSerializer.Deserialize<NotificationSchedule>(db.ScheduleJson)
              ?? NotificationSchedule.Immediate();

        var recipient = new NotificationRecipient
        {
            Channel = (NotificationChannel)db.Channel,
            Address = db.RecipientAddress,
            DisplayName = db.RecipientDisplayName,
            Metadata = recipientMetadata
        };

        var content = new NotificationContent
        {
            Subject = db.ContentSubject,
            Body = db.ContentBody,
            TemplateId = db.ContentTemplateId,
            TemplateData = templateData
        };

        var attempts = db.DeliveryAttempts
            .OrderBy(a => a.AttemptNumber)
            .Select(a => new DeliveryAttempt
            {
                Channel = (NotificationChannel)a.Channel,
                Status = (DeliveryStatus)a.Status,
                AttemptedAt = new DateTimeOffset(a.AttemptedAt, TimeSpan.Zero),
                AttemptNumber = a.AttemptNumber,
                ProviderMessageId = a.ProviderMessageId,
                FailureReason = a.FailureReason,
                NextRetryAfter = a.NextRetryAfter
            })
            .ToList();

        return Notification.Rehydrate(
            UlidId.Parse(db.Id),
            UlidId.Parse(db.CustomerId),
            db.OrderId is not null ? UlidId.Parse(db.OrderId) : null,
            db.SubjectId is not null ? UlidId.Parse(db.SubjectId) : null,
            (NotificationTriggerType)db.TriggerType,
            recipient,
            content,
            schedule,
            (NotificationPriority)db.Priority,
            (DeliveryStatus)db.Status,
            db.IsAdverseAction,
            new DateTimeOffset(db.CreatedAt, TimeSpan.Zero),
            db.ScheduledFor.HasValue ? new DateTimeOffset(db.ScheduledFor.Value, TimeSpan.Zero) : null,
            db.ProcessedAt.HasValue ? new DateTimeOffset(db.ProcessedAt.Value, TimeSpan.Zero) : null,
            db.DeliveredAt.HasValue ? new DateTimeOffset(db.DeliveredAt.Value, TimeSpan.Zero) : null,
            db.CorrelationId,
            attempts);
    }

    public static NotificationDb ToDb(Notification notification)
    {
        return new NotificationDb
        {
            Id = notification.Id.ToString(),
            CustomerId = notification.CustomerId.ToString(),
            OrderId = notification.OrderId?.ToString(),
            SubjectId = notification.SubjectId?.ToString(),
            TriggerType = (int)notification.TriggerType,
            Channel = (int)notification.Recipient.Channel,
            RecipientAddress = notification.Recipient.Address,
            RecipientDisplayName = notification.Recipient.DisplayName,
            RecipientMetadataJson = JsonSerializer.Serialize(notification.Recipient.Metadata),
            ContentSubject = notification.Content.Subject,
            ContentBody = notification.Content.Body,
            ContentTemplateId = notification.Content.TemplateId,
            ContentTemplateDataJson = JsonSerializer.Serialize(notification.Content.TemplateData),
            ScheduleJson = JsonSerializer.Serialize(notification.Schedule),
            ScheduledFor = notification.ScheduledFor?.UtcDateTime,
            Priority = (int)notification.Priority,
            Status = (int)notification.Status,
            IsAdverseAction = notification.IsAdverseAction,
            CreatedAt = notification.CreatedAt.UtcDateTime,
            ProcessedAt = notification.ProcessedAt?.UtcDateTime,
            DeliveredAt = notification.DeliveredAt?.UtcDateTime,
            CorrelationId = notification.CorrelationId,
            DeliveryAttempts = notification.DeliveryAttempts
                .Select(a => new DeliveryAttemptDb
                {
                    NotificationId = notification.Id.ToString(),
                    Channel = (int)a.Channel,
                    Status = (int)a.Status,
                    AttemptedAt = a.AttemptedAt.UtcDateTime,
                    AttemptNumber = a.AttemptNumber,
                    ProviderMessageId = a.ProviderMessageId,
                    FailureReason = a.FailureReason,
                    NextRetryAfter = a.NextRetryAfter
                })
                .ToList()
        };
    }

    public static void UpdateDb(NotificationDb db, Notification notification)
    {
        db.Status = (int)notification.Status;
        db.ProcessedAt = notification.ProcessedAt?.UtcDateTime;
        db.DeliveredAt = notification.DeliveredAt?.UtcDateTime;

        // Sync delivery attempts
        var existingAttemptNumbers = db.DeliveryAttempts.Select(a => a.AttemptNumber).ToHashSet();
        foreach (var attempt in notification.DeliveryAttempts)
        {
            if (!existingAttemptNumbers.Contains(attempt.AttemptNumber))
            {
                db.DeliveryAttempts.Add(new DeliveryAttemptDb
                {
                    NotificationId = notification.Id.ToString(),
                    Channel = (int)attempt.Channel,
                    Status = (int)attempt.Status,
                    AttemptedAt = attempt.AttemptedAt.UtcDateTime,
                    AttemptNumber = attempt.AttemptNumber,
                    ProviderMessageId = attempt.ProviderMessageId,
                    FailureReason = attempt.FailureReason,
                    NextRetryAfter = attempt.NextRetryAfter
                });
            }
            else
            {
                var existingAttempt = db.DeliveryAttempts.First(a => a.AttemptNumber == attempt.AttemptNumber);
                existingAttempt.Status = (int)attempt.Status;
                existingAttempt.ProviderMessageId = attempt.ProviderMessageId;
                existingAttempt.FailureReason = attempt.FailureReason;
                existingAttempt.NextRetryAfter = attempt.NextRetryAfter;
            }
        }
    }
}