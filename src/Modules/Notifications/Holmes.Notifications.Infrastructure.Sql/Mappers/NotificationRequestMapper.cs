using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.ValueObjects;
using Holmes.Notifications.Infrastructure.Sql.Entities;

namespace Holmes.Notifications.Infrastructure.Sql.Mappers;

public static class NotificationRequestMapper
{
    public static NotificationRequest ToDomain(NotificationRequestDb db)
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

        return NotificationRequest.Rehydrate(
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

    public static NotificationRequestDb ToDb(NotificationRequest request)
    {
        return new NotificationRequestDb
        {
            Id = request.Id.ToString(),
            CustomerId = request.CustomerId.ToString(),
            OrderId = request.OrderId?.ToString(),
            SubjectId = request.SubjectId?.ToString(),
            TriggerType = (int)request.TriggerType,
            Channel = (int)request.Recipient.Channel,
            RecipientAddress = request.Recipient.Address,
            RecipientDisplayName = request.Recipient.DisplayName,
            RecipientMetadataJson = JsonSerializer.Serialize(request.Recipient.Metadata),
            ContentSubject = request.Content.Subject,
            ContentBody = request.Content.Body,
            ContentTemplateId = request.Content.TemplateId,
            ContentTemplateDataJson = JsonSerializer.Serialize(request.Content.TemplateData),
            ScheduleJson = JsonSerializer.Serialize(request.Schedule),
            ScheduledFor = request.ScheduledFor?.UtcDateTime,
            Priority = (int)request.Priority,
            Status = (int)request.Status,
            IsAdverseAction = request.IsAdverseAction,
            CreatedAt = request.CreatedAt.UtcDateTime,
            ProcessedAt = request.ProcessedAt?.UtcDateTime,
            DeliveredAt = request.DeliveredAt?.UtcDateTime,
            CorrelationId = request.CorrelationId,
            DeliveryAttempts = request.DeliveryAttempts
                .Select(a => new DeliveryAttemptDb
                {
                    NotificationRequestId = request.Id.ToString(),
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
}