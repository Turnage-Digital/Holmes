using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Services.Domain;

public interface IServiceRequestRepository
{
    Task<ServiceRequest?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> GetByOrderIdAsync(UlidId orderId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> GetPendingByTierAsync(
        UlidId orderId,
        int tier,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> GetPendingForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ServiceRequest>> GetRetryableAsync(
        int batchSize,
        CancellationToken cancellationToken = default);

    Task<ServiceRequest?> GetByVendorReferenceAsync(
        string vendorCode,
        string vendorReferenceId,
        CancellationToken cancellationToken = default);

    Task<bool> AllCompletedForOrderAsync(UlidId orderId, CancellationToken cancellationToken = default);

    Task<bool> TierCompletedAsync(UlidId orderId, int tier, CancellationToken cancellationToken = default);

    void Add(ServiceRequest request);

    void Update(ServiceRequest request);
}
