using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Domain;

public interface IServiceRequestRepository
{
    Task<Service?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Service>> GetByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Service>> GetPendingByTierAsync(
        UlidId orderId,
        int tier,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Service>> GetPendingForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<Service>> GetRetryableAsync(
        int batchSize,
        CancellationToken cancellationToken = default
    );

    Task<Service?> GetByVendorReferenceAsync(
        string vendorCode,
        string vendorReferenceId,
        CancellationToken cancellationToken = default
    );

    Task<bool> AllCompletedForOrderAsync(UlidId orderId, CancellationToken cancellationToken = default);

    Task<bool> TierCompletedAsync(UlidId orderId, int tier, CancellationToken cancellationToken = default);

    void Add(Service request);

    void Update(Service request);
}