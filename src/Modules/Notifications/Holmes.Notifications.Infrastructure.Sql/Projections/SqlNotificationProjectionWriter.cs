using Holmes.Notifications.Application.Abstractions.Projections;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Notifications.Infrastructure.Sql.Projections;

public sealed class SqlNotificationProjectionWriter(
    NotificationsDbContext dbContext,
    ILogger<SqlNotificationProjectionWriter> logger
) : INotificationProjectionWriter
{
    public async Task UpsertAsync(NotificationProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.NotificationProjections
            .FirstOrDefaultAsync(x => x.Id == model.NotificationId, cancellationToken);

        if (record is null)
        {
            record = new NotificationProjectionDb
            {
                Id = model.NotificationId
            };
            dbContext.NotificationProjections.Add(record);
        }

        record.CustomerId = model.CustomerId;
        record.OrderId = model.OrderId;
        record.SubjectId = model.SubjectId;
        record.TriggerType = (int)model.TriggerType;
        record.Channel = (int)model.Channel;
        record.Status = (int)model.Status;
        record.IsAdverseAction = model.IsAdverseAction;
        record.CreatedAt = model.CreatedAt.UtcDateTime;
        record.ScheduledFor = model.ScheduledFor?.UtcDateTime;
        record.AttemptCount = 0;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateQueuedAsync(
        string notificationId,
        DateTimeOffset queuedAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.NotificationProjections
            .FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Notification projection not found for queued update: {NotificationId}", notificationId);
            return;
        }

        record.Status = (int)DeliveryStatus.Queued;
        record.QueuedAt = queuedAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDeliveredAsync(
        string notificationId,
        DateTimeOffset deliveredAt,
        string? providerMessageId,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.NotificationProjections
            .FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Notification projection not found for delivery update: {NotificationId}",
                notificationId);
            return;
        }

        record.Status = (int)DeliveryStatus.Delivered;
        record.DeliveredAt = deliveredAt.UtcDateTime;
        record.ProviderMessageId = providerMessageId;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateFailedAsync(
        string notificationId,
        DateTimeOffset failedAt,
        string reason,
        int attemptNumber,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.NotificationProjections
            .FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Notification projection not found for failure update: {NotificationId}", notificationId);
            return;
        }

        record.Status = (int)DeliveryStatus.Failed;
        record.FailedAt = failedAt.UtcDateTime;
        record.LastFailureReason = reason;
        record.AttemptCount = attemptNumber;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBouncedAsync(
        string notificationId,
        DateTimeOffset bouncedAt,
        string reason,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.NotificationProjections
            .FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Notification projection not found for bounce update: {NotificationId}", notificationId);
            return;
        }

        record.Status = (int)DeliveryStatus.Bounced;
        record.BouncedAt = bouncedAt.UtcDateTime;
        record.LastFailureReason = reason;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCancelledAsync(
        string notificationId,
        DateTimeOffset cancelledAt,
        string reason,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.NotificationProjections
            .FirstOrDefaultAsync(x => x.Id == notificationId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("Notification projection not found for cancel update: {NotificationId}", notificationId);
            return;
        }

        record.Status = (int)DeliveryStatus.Cancelled;
        record.CancelledAt = cancelledAt.UtcDateTime;
        record.LastFailureReason = reason;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}