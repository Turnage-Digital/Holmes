using Holmes.Notifications.Application.Abstractions.Dtos;
using Holmes.Notifications.Domain;

namespace Holmes.Notifications.Application.Abstractions;

/// <summary>
///     Query interface for notification lookups. Used by application layer for read operations.
/// </summary>
public interface INotificationQueries
{
    /// <summary>
    ///     Gets a notification by its ID.
    /// </summary>
    Task<NotificationSummaryDto?> GetByIdAsync(
        string notificationId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets pending notifications ready for processing.
    /// </summary>
    Task<IReadOnlyList<NotificationPendingDto>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets all notifications for an order.
    /// </summary>
    Task<IReadOnlyList<NotificationSummaryDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets failed notifications eligible for retry.
    /// </summary>
    Task<IReadOnlyList<NotificationPendingDto>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken
    );
}

/// <summary>
///     DTO for send/retry operations (contains Id for re-fetch via repository).
/// </summary>
public sealed record NotificationPendingDto(
    string Id,
    string CustomerId,
    string? OrderId,
    NotificationTriggerType TriggerType,
    NotificationChannel Channel,
    string RecipientAddress,
    DeliveryStatus Status,
    int AttemptCount,
    DateTimeOffset? ScheduledFor
);