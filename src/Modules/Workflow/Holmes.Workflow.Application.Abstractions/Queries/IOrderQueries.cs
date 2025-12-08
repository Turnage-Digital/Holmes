using Holmes.Workflow.Application.Abstractions.Dtos;

namespace Holmes.Workflow.Application.Abstractions.Queries;

/// <summary>
///     Query interface for order lookups. Used by application layer for read operations.
/// </summary>
public interface IOrderQueries
{
    /// <summary>
    ///     Gets an order summary by ID.
    /// </summary>
    Task<OrderSummaryDto?> GetSummaryByIdAsync(string orderId, CancellationToken cancellationToken);

    /// <summary>
    ///     Gets paginated order summaries with filtering.
    /// </summary>
    Task<OrderSummaryPagedResult> GetSummariesPagedAsync(
        OrderSummaryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets order statistics grouped by status.
    /// </summary>
    Task<OrderStatsDto> GetStatsAsync(
        IReadOnlyCollection<string>? allowedCustomerIds,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets the timeline events for an order.
    /// </summary>
    Task<IReadOnlyList<OrderTimelineEntryDto>> GetTimelineAsync(
        string orderId,
        DateTimeOffset? before,
        int limit,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets the customer ID for an order (for access control).
    /// </summary>
    Task<string?> GetCustomerIdAsync(string orderId, CancellationToken cancellationToken);
}

/// <summary>
///     Filter for order summary queries.
/// </summary>
public sealed record OrderSummaryFilter(
    IReadOnlyCollection<string>? AllowedCustomerIds,
    string? OrderId,
    string? SubjectId,
    string? CustomerId,
    IReadOnlyCollection<string>? Statuses
);

/// <summary>
///     Paginated result for order summary queries.
/// </summary>
public sealed record OrderSummaryPagedResult(
    IReadOnlyList<OrderSummaryDto> Items,
    int TotalCount
);