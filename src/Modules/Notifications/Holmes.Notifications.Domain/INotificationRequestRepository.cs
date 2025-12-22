using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Notifications.Domain;

public interface INotificationRequestRepository
{
    Task<Notification?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> GetPendingAsync(
        int limit,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Notification>> GetByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Notification>> GetFailedForRetryAsync(
        int maxAttempts,
        TimeSpan retryAfter,
        int limit,
        CancellationToken cancellationToken = default
    );

    Task AddAsync(Notification request, CancellationToken cancellationToken = default);

    Task UpdateAsync(Notification request, CancellationToken cancellationToken = default);
}