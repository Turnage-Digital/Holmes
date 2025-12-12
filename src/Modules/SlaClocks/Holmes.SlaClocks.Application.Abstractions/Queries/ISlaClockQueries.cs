using Holmes.SlaClocks.Application.Abstractions.Dtos;
using Holmes.SlaClocks.Domain;

namespace Holmes.SlaClocks.Application.Abstractions.Queries;

/// <summary>
///     Query interface for SLA clock lookups. Used by application layer for read operations.
/// </summary>
public interface ISlaClockQueries
{
    /// <summary>
    ///     Gets an SLA clock by its ID.
    /// </summary>
    Task<SlaClockDto?> GetByIdAsync(
        string clockId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets all SLA clocks for an order.
    /// </summary>
    Task<IReadOnlyList<SlaClockDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets an SLA clock by order ID and kind.
    /// </summary>
    Task<SlaClockDto?> GetByOrderIdAndKindAsync(
        string orderId,
        ClockKind kind,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets active (non-completed, non-breached) SLA clocks for an order.
    /// </summary>
    Task<IReadOnlyList<SlaClockDto>> GetActiveByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets running clocks that have passed their at-risk threshold but not yet marked as at-risk.
    ///     Used by watchdog service.
    /// </summary>
    Task<IReadOnlyList<SlaClockWatchdogDto>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Gets running or at-risk clocks that have passed their deadline but not yet marked as breached.
    ///     Used by watchdog service.
    /// </summary>
    Task<IReadOnlyList<SlaClockWatchdogDto>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    );
}

/// <summary>
///     DTO for watchdog operations (contains Id for re-fetch via repository).
/// </summary>
public sealed record SlaClockWatchdogDto(
    string Id,
    string OrderId,
    string CustomerId,
    ClockKind Kind,
    ClockState State,
    DateTimeOffset StartedAt,
    DateTimeOffset DeadlineAt,
    DateTimeOffset AtRiskThresholdAt
);