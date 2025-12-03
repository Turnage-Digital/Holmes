using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Notifications.Domain;

public interface INotificationRequestRepository
{
    Task<NotificationRequest?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NotificationRequest>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<NotificationRequest>> GetByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<NotificationRequest>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(NotificationRequest request, CancellationToken cancellationToken = default);

    Task UpdateAsync(NotificationRequest request, CancellationToken cancellationToken = default);
}