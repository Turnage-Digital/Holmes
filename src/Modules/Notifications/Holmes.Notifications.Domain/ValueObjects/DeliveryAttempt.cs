namespace Holmes.Notifications.Domain.ValueObjects;

public sealed record DeliveryAttempt
{
    public NotificationChannel Channel { get; init; }
    public DeliveryStatus Status { get; init; }
    public DateTimeOffset AttemptedAt { get; init; }
    public int AttemptNumber { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? FailureReason { get; init; }
    public TimeSpan? NextRetryAfter { get; init; }

    public static DeliveryAttempt Success(
        NotificationChannel channel,
        DateTimeOffset attemptedAt,
        int attemptNumber,
        string? providerMessageId = null
    )
    {
        return new DeliveryAttempt
        {
            Channel = channel,
            Status = DeliveryStatus.Delivered,
            AttemptedAt = attemptedAt,
            AttemptNumber = attemptNumber,
            ProviderMessageId = providerMessageId
        };
    }

    public static DeliveryAttempt Failure(
        NotificationChannel channel,
        DateTimeOffset attemptedAt,
        int attemptNumber,
        string failureReason,
        TimeSpan? retryAfter = null
    )
    {
        return new DeliveryAttempt
        {
            Channel = channel,
            Status = DeliveryStatus.Failed,
            AttemptedAt = attemptedAt,
            AttemptNumber = attemptNumber,
            FailureReason = failureReason,
            NextRetryAfter = retryAfter
        };
    }

    public static DeliveryAttempt Bounced(
        NotificationChannel channel,
        DateTimeOffset attemptedAt,
        int attemptNumber,
        string reason
    )
    {
        return new DeliveryAttempt
        {
            Channel = channel,
            Status = DeliveryStatus.Bounced,
            AttemptedAt = attemptedAt,
            AttemptNumber = attemptNumber,
            FailureReason = reason
        };
    }
}