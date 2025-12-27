namespace Holmes.Notifications.Infrastructure.Sql.Entities;

public class NotificationDb
{
    public string Id { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string? OrderId { get; set; }
    public string? SubjectId { get; set; }
    public int TriggerType { get; set; }
    public int Channel { get; set; }
    public string RecipientAddress { get; set; } = null!;
    public string? RecipientDisplayName { get; set; }
    public string RecipientMetadataJson { get; set; } = "{}";
    public string ContentSubject { get; set; } = null!;
    public string ContentBody { get; set; } = null!;
    public string? ContentTemplateId { get; set; }
    public string ContentTemplateDataJson { get; set; } = "{}";
    public string ScheduleJson { get; set; } = "{}";
    public int Priority { get; set; }
    public int Status { get; set; }
    public bool IsAdverseAction { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? ProcessedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public string? CorrelationId { get; set; }

    public ICollection<DeliveryAttemptDb> DeliveryAttempts { get; set; } = new List<DeliveryAttemptDb>();
}

public class DeliveryAttemptDb
{
    public int Id { get; set; }
    public string NotificationId { get; set; } = null!;
    public int Channel { get; set; }
    public int Status { get; set; }
    public DateTime AttemptedAt { get; set; }
    public int AttemptNumber { get; set; }
    public string? ProviderMessageId { get; set; }
    public string? FailureReason { get; set; }
    public TimeSpan? NextRetryAfter { get; set; }

    public NotificationDb Notification { get; set; } = null!;
}