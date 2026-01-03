using Holmes.Services.Contracts.Dtos;
using Holmes.Services.Domain;

// ReSharper disable once CheckNamespace

namespace Holmes.Services.Application.Queries;

/// <summary>
///     Query interface for service lookups. Used by application layer for read operations.
/// </summary>
public interface IServiceQueries
{
    /// <summary>
    ///     Gets a service by ID.
    /// </summary>
    Task<ServiceSummaryDto?> GetByIdAsync(
        string serviceId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets all services for an order.
    /// </summary>
    Task<IReadOnlyList<ServiceSummaryDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets pending services for a specific tier.
    /// </summary>
    Task<IReadOnlyList<ServiceDispatchDto>> GetPendingByTierAsync(
        string orderId,
        int tier,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets pending services ready for dispatch.
    /// </summary>
    Task<IReadOnlyList<ServiceDispatchDto>> GetPendingForDispatchAsync(
        int batchSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets failed services eligible for retry.
    /// </summary>
    Task<IReadOnlyList<ServiceDispatchDto>> GetRetryableAsync(
        int batchSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets a service by vendor reference.
    /// </summary>
    Task<ServiceDispatchDto?> GetByVendorReferenceAsync(
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
    ///     Gets paginated services for the fulfillment queue (pending and in-progress).
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
public sealed record ServiceDispatchDto(
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
    IReadOnlyCollection<ServiceStatus>? Statuses,
    IReadOnlyCollection<ServiceCategory>? Categories
);

/// <summary>
///     Paginated result for service fulfillment queue.
/// </summary>
public sealed record ServiceFulfillmentQueuePagedResult(
    IReadOnlyList<ServiceSummaryDto> Items,
    int TotalCount
);