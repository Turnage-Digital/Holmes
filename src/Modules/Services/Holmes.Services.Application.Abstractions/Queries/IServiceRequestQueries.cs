using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Domain;

// ReSharper disable once CheckNamespace

namespace Holmes.Services.Application.Abstractions.Queries;

/// <summary>
///     Query interface for service request lookups. Used by application layer for read operations.
/// </summary>
public interface IServiceRequestQueries
{
    /// <summary>
    ///     Gets a service request by ID.
    /// </summary>
    Task<ServiceRequestSummaryDto?> GetByIdAsync(
        string serviceRequestId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets all service requests for an order.
    /// </summary>
    Task<IReadOnlyList<ServiceRequestSummaryDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets pending service requests for a specific tier.
    /// </summary>
    Task<IReadOnlyList<ServiceRequestDispatchDto>> GetPendingByTierAsync(
        string orderId,
        int tier,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets pending service requests ready for dispatch.
    /// </summary>
    Task<IReadOnlyList<ServiceRequestDispatchDto>> GetPendingForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets failed service requests eligible for retry.
    /// </summary>
    Task<IReadOnlyList<ServiceRequestDispatchDto>> GetRetryableAsync(
        int batchSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets a service request by vendor reference.
    /// </summary>
    Task<ServiceRequestDispatchDto?> GetByVendorReferenceAsync(
        string vendorCode,
        string vendorReferenceId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Checks if all services for an order are completed.
    /// </summary>
    Task<bool> AllCompletedForOrderAsync(string orderId, CancellationToken cancellationToken);

    /// <summary>
    ///     Checks if all services in a tier are completed.
    /// </summary>
    Task<bool> TierCompletedAsync(string orderId, int tier, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets order completion status with detailed counts.
    /// </summary>
    Task<OrderCompletionStatusDto> GetOrderCompletionStatusAsync(
        string orderId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets paginated service requests for the fulfillment queue (pending and in-progress).
    /// </summary>
    Task<ServiceFulfillmentQueuePagedResult> GetFulfillmentQueuePagedAsync(
        ServiceFulfillmentQueueFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}

/// <summary>
///     DTO for dispatch operations (contains Id for re-fetch).
/// </summary>
public sealed record ServiceRequestDispatchDto(
    string Id,
    string OrderId,
    string CustomerId,
    string ServiceTypeCode,
    int Tier,
    string? VendorCode,
    ServiceStatus Status,
    int AttemptCount,
    int MaxAttempts
);

/// <summary>
///     DTO for order completion status.
/// </summary>
public sealed record OrderCompletionStatusDto(
    string OrderId,
    int TotalServices,
    int CompletedServices,
    int PendingServices,
    int FailedServices,
    int CanceledServices,
    bool AllCompleted
);

/// <summary>
///     Filter for service fulfillment queue queries.
/// </summary>
public sealed record ServiceFulfillmentQueueFilter(
    IReadOnlyCollection<string>? AllowedCustomerIds,
    string? CustomerId,
    IReadOnlyCollection<ServiceStatus>? Statuses
);

/// <summary>
///     Paginated result for service fulfillment queue.
/// </summary>
public sealed record ServiceFulfillmentQueuePagedResult(
    IReadOnlyList<ServiceRequestSummaryDto> Items,
    int TotalCount
);