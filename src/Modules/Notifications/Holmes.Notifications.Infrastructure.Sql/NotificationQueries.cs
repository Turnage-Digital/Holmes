using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Notifications.Application.Abstractions;
using Holmes.Notifications.Application.Abstractions.Dtos;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Notifications.Infrastructure.Sql;

public sealed class NotificationQueries(NotificationsDbContext dbContext) 
    : INotificationQueries
{
    public async Task<NotificationSummaryDto?> GetByIdAsync(
        string notificationId,
        CancellationToken cancellationToken
    )
    {
        var notification = await dbContext.Notifications
            .AsNoTracking()
            .Where(n => n.Id == notificationId)
            .Select(n => new NotificationSummaryDto(
                UlidId.Parse(n.Id),
                UlidId.Parse(n.CustomerId),
                n.OrderId != null ? UlidId.Parse(n.OrderId) : null,
                (NotificationTriggerType)n.TriggerType,
                (NotificationChannel)n.Channel,
                n.RecipientAddress,
                (DeliveryStatus)n.Status,
                n.IsAdverseAction,
                new DateTimeOffset(n.CreatedAt, TimeSpan.Zero),
                n.DeliveredAt.HasValue ? new DateTimeOffset(n.DeliveredAt.Value, TimeSpan.Zero) : null,
                n.DeliveryAttempts.Count
            ))
            .FirstOrDefaultAsync(cancellationToken);

        return notification;
    }

    public async Task<IReadOnlyList<NotificationPendingDto>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken
    )
    {
        var spec = new PendingNotificationsSpec(DateTime.UtcNow, limit);

        return await dbContext.Notifications
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(n => new NotificationPendingDto(
                n.Id,
                n.CustomerId,
                n.OrderId,
                (NotificationTriggerType)n.TriggerType,
                (NotificationChannel)n.Channel,
                n.RecipientAddress,
                (DeliveryStatus)n.Status,
                n.DeliveryAttempts.Count,
                n.ScheduledFor.HasValue ? new DateTimeOffset(n.ScheduledFor.Value, TimeSpan.Zero) : null
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationSummaryDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    )
    {
        var spec = new NotificationsByOrderIdSpec(orderId);

        return await dbContext.Notifications
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(n => new NotificationSummaryDto(
                UlidId.Parse(n.Id),
                UlidId.Parse(n.CustomerId),
                n.OrderId != null ? UlidId.Parse(n.OrderId) : null,
                (NotificationTriggerType)n.TriggerType,
                (NotificationChannel)n.Channel,
                n.RecipientAddress,
                (DeliveryStatus)n.Status,
                n.IsAdverseAction,
                new DateTimeOffset(n.CreatedAt, TimeSpan.Zero),
                n.DeliveredAt.HasValue ? new DateTimeOffset(n.DeliveredAt.Value, TimeSpan.Zero) : null,
                n.DeliveryAttempts.Count
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<NotificationPendingDto>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var cutoff = DateTime.UtcNow.Subtract(retryAfter);
        var spec = new FailedNotificationsForRetrySpec(maxAttempts, cutoff, limit);

        return await dbContext.Notifications
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(n => new NotificationPendingDto(
                n.Id,
                n.CustomerId,
                n.OrderId,
                (NotificationTriggerType)n.TriggerType,
                (NotificationChannel)n.Channel,
                n.RecipientAddress,
                (DeliveryStatus)n.Status,
                n.DeliveryAttempts.Count,
                n.ScheduledFor.HasValue ? new DateTimeOffset(n.ScheduledFor.Value, TimeSpan.Zero) : null
            ))
            .ToListAsync(cancellationToken);
    }
}