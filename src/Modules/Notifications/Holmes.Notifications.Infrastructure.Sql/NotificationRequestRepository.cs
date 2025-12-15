using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql.Mappers;
using Holmes.Notifications.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Notifications.Infrastructure.Sql;

public sealed class NotificationRequestRepository(NotificationsDbContext context) : INotificationRequestRepository
{
    public async Task<NotificationRequest?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
    {
        var db = await context.NotificationRequests
            .Include(n => n.DeliveryAttempts)
            .FirstOrDefaultAsync(n => n.Id == id.ToString(), cancellationToken);

        return db is null ? null : NotificationRequestMapper.ToDomain(db);
    }

    public async Task<IReadOnlyList<NotificationRequest>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new PendingNotificationsSpec(DateTime.UtcNow, limit);

        var pending = await context.NotificationRequests
            .Include(n => n.DeliveryAttempts)
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return pending.Select(NotificationRequestMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<NotificationRequest>> GetByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new NotificationsByOrderIdSpec(orderId.ToString());

        var notifications = await context.NotificationRequests
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return notifications.Select(NotificationRequestMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<NotificationRequest>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken = default
    )
    {
        var cutoff = DateTime.UtcNow.Subtract(retryAfter);
        var spec = new FailedNotificationsForRetrySpec(maxAttempts, cutoff, limit);

        var failed = await context.NotificationRequests
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return failed.Select(NotificationRequestMapper.ToDomain).ToList();
    }

    public async Task AddAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var db = NotificationRequestMapper.ToDb(request);
        await context.NotificationRequests.AddAsync(db, cancellationToken);
    }

    public async Task UpdateAsync(NotificationRequest request, CancellationToken cancellationToken = default)
    {
        var db = await context.NotificationRequests
            .Include(n => n.DeliveryAttempts)
            .FirstOrDefaultAsync(n => n.Id == request.Id.ToString(), cancellationToken);

        if (db is null)
        {
            throw new InvalidOperationException($"Notification request '{request.Id}' not found.");
        }

        NotificationRequestMapper.UpdateDb(db, request);
    }
}