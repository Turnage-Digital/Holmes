using Holmes.Notifications.Domain;

namespace Holmes.Notifications.Contracts;

/// <summary>
///     Writes notification projection data for read model queries.
///     Called by event handlers to keep projections in sync.
/// </summary>
public interface INotificationProjectionWriter
{
    /// <summary>
    ///     Inserts or updates a full notification projection record.
    ///     Called on NotificationCreated events.
    /// </summary>
    Task UpsertAsync(NotificationProjectionModel model, CancellationToken cancellationToken);

    /// <summary>
    ///     Updates the status to Queued. Called on NotificationQueued events.
    /// </summary>
    Task UpdateQueuedAsync(
        string notificationId,
        DateTimeOffset queuedAt,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates the status to Delivered. Called on NotificationDelivered events.
    /// </summary>
    Task UpdateDeliveredAsync(
        string notificationId,
        DateTimeOffset deliveredAt,
        string? providerMessageId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates the status to Failed. Called on NotificationDeliveryFailed events.
    /// </summary>
    Task UpdateFailedAsync(
        string notificationId,
        DateTimeOffset failedAt,
        string reason,
        int attemptNumber,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates the status to Bounced. Called on NotificationBounced events.
    /// </summary>
    Task UpdateBouncedAsync(
        string notificationId,
        DateTimeOffset bouncedAt,
        string reason,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates the status to Cancelled. Called on NotificationCancelled events.
    /// </summary>
    Task UpdateCancelledAsync(
        string notificationId,
        DateTimeOffset cancelledAt,
        string reason,
        CancellationToken cancellationToken
    );
}

/// <summary>
///     Model representing the full notification projection data.
/// </summary>
public sealed record NotificationProjectionModel(
    string NotificationId,
    string CustomerId,
    string? OrderId,
    string? SubjectId,
    NotificationTriggerType TriggerType,
    NotificationChannel Channel,
    DeliveryStatus Status,
    bool IsAdverseAction,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor
);