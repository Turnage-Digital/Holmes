namespace Holmes.Notifications.Infrastructure.Sql.Entities;

/// <summary>
///     Database entity for the notification read-model projection.
///     Populated by event handlers for fast query access.
/// </summary>
public class NotificationProjectionDb
{
    public string Id { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string? OrderId { get; set; }
    public string? SubjectId { get; set; }
    public int TriggerType { get; set; }
    public int Channel { get; set; }
    public int Status { get; set; }
    public bool IsAdverseAction { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ScheduledFor { get; set; }
    public DateTime? QueuedAt { get; set; }
    public DateTime? DeliveredAt { get; set; }
    public DateTime? FailedAt { get; set; }
    public DateTime? BouncedAt { get; set; }
    public DateTime? CancelledAt { get; set; }
    public int AttemptCount { get; set; }
    public string? LastFailureReason { get; set; }
    public string? ProviderMessageId { get; set; }
}
