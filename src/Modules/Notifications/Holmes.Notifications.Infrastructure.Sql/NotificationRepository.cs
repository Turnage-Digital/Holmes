using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql.Mappers;
using Holmes.Notifications.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Notifications.Infrastructure.Sql;

public sealed class NotificationRepository(NotificationsDbContext context) : INotificationRepository
{
    public async Task<Notification?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
    {
        var db = await context.Notifications
            .Include(n => n.DeliveryAttempts)
            .FirstOrDefaultAsync(n => n.Id == id.ToString(), cancellationToken);

        return db is null ? null : NotificationMapper.ToDomain(db);
    }

    public async Task<IReadOnlyList<Notification>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new PendingNotificationsSpec(DateTime.UtcNow, limit);

        var pending = await context.Notifications
            .Include(n => n.DeliveryAttempts)
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return pending.Select(NotificationMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Notification>> GetByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new NotificationsByOrderIdSpec(orderId.ToString());

        var notifications = await context.Notifications
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return notifications.Select(NotificationMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<Notification>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var cutoff = DateTime.UtcNow.Subtract(retryAfter);
        var spec = new FailedNotificationsForRetrySpec(maxAttempts, cutoff, limit);

        var failed = await context.Notifications
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return failed.Select(NotificationMapper.ToDomain).ToList();
    }

    public async Task AddAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var db = NotificationMapper.ToDb(notification);
        await context.Notifications.AddAsync(db, cancellationToken);
    }

    public async Task UpdateAsync(Notification notification, CancellationToken cancellationToken = default)
    {
        var db = await context.Notifications
            .Include(n => n.DeliveryAttempts)
            .FirstOrDefaultAsync(n => n.Id == notification.Id.ToString(), cancellationToken);

        if (db is null)
        {
            throw new InvalidOperationException($"Notification '{notification.Id}' not found.");
        }

        NotificationMapper.UpdateDb(db, notification);
    }
}