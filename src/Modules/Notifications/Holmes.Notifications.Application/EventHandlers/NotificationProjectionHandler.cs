using Holmes.Notifications.Application.Abstractions.Projections;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.Events;
using MediatR;

namespace Holmes.Notifications.Application.EventHandlers;

/// <summary>
///     Handles notification domain events to maintain the notification_projections table.
/// </summary>
public sealed class NotificationProjectionHandler(
    INotificationProjectionWriter writer
)
    : INotificationHandler<NotificationCreated>,
        INotificationHandler<NotificationQueued>,
        INotificationHandler<NotificationDelivered>,
        INotificationHandler<NotificationDeliveryFailed>,
        INotificationHandler<NotificationBounced>,
        INotificationHandler<NotificationCancelled>
{
    public Task Handle(NotificationBounced notification, CancellationToken cancellationToken)
    {
        return writer.UpdateBouncedAsync(
            notification.NotificationId.ToString(),
            notification.BouncedAt,
            notification.Reason,
            cancellationToken);
    }

    public Task Handle(NotificationCancelled notification, CancellationToken cancellationToken)
    {
        return writer.UpdateCancelledAsync(
            notification.NotificationId.ToString(),
            notification.CancelledAt,
            notification.Reason,
            cancellationToken);
    }

    public Task Handle(NotificationDelivered notification, CancellationToken cancellationToken)
    {
        return writer.UpdateDeliveredAsync(
            notification.NotificationId.ToString(),
            notification.DeliveredAt,
            notification.ProviderMessageId,
            cancellationToken);
    }

    public Task Handle(NotificationDeliveryFailed notification, CancellationToken cancellationToken)
    {
        return writer.UpdateFailedAsync(
            notification.NotificationId.ToString(),
            notification.FailedAt,
            notification.Reason,
            notification.AttemptNumber,
            cancellationToken);
    }

    public Task Handle(NotificationQueued notification, CancellationToken cancellationToken)
    {
        return writer.UpdateQueuedAsync(
            notification.NotificationId.ToString(),
            notification.QueuedAt,
            cancellationToken);
    }

    public Task Handle(NotificationCreated notification, CancellationToken cancellationToken)
    {
        var model = new NotificationProjectionModel(
            notification.NotificationId.ToString(),
            notification.CustomerId.ToString(),
            notification.OrderId?.ToString(),
            notification.SubjectId?.ToString(),
            notification.TriggerType,
            notification.Channel,
            DeliveryStatus.Pending,
            notification.IsAdverseAction,
            notification.CreatedAt,
            notification.ScheduledFor
        );

        return writer.UpsertAsync(model, cancellationToken);
    }
}