using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Notifications.Application.EventHandlers;

public sealed class ProcessImmediateNotificationHandler(
    ISender sender,
    ILogger<ProcessImmediateNotificationHandler> logger
) : INotificationHandler<NotificationRequestCreated>
{
    public async Task Handle(NotificationRequestCreated notification, CancellationToken cancellationToken)
    {
        // Only process immediately if not scheduled for later
        if (notification.ScheduledFor.HasValue)
        {
            logger.LogDebug(
                "Notification {NotificationId} is scheduled for {ScheduledFor}, skipping immediate processing",
                notification.NotificationId,
                notification.ScheduledFor);
            return;
        }

        logger.LogDebug(
            "Processing immediate notification {NotificationId}",
            notification.NotificationId);

        try
        {
            var result = await sender.Send(
                new ProcessNotificationCommand(notification.NotificationId),
                cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogDebug(
                    "Immediate notification {NotificationId} processed successfully",
                    notification.NotificationId);
            }
            else
            {
                // Don't throw - the background service will retry
                logger.LogWarning(
                    "Immediate notification {NotificationId} processing failed: {Error}. Will be retried by background service.",
                    notification.NotificationId,
                    result.Error);
            }
        }
        catch (Exception ex)
        {
            // Don't throw - the background service will retry
            logger.LogWarning(
                ex,
                "Exception processing immediate notification {NotificationId}. Will be retried by background service.",
                notification.NotificationId);
        }
    }
}