using Holmes.Core.Domain.ValueObjects;

namespace Holmes.SlaClocks.Domain;

public interface ISlaClockRepository
{
    Task<SlaClock?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlaClock>> GetByOrderIdAsync(UlidId orderId, CancellationToken cancellationToken = default);

    Task<SlaClock?> GetByOrderIdAndKindAsync(
        UlidId orderId,
        ClockKind kind,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SlaClock>> GetActiveByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets running clocks that have passed their at-risk threshold but not yet marked as at-risk.
    /// </summary>
    Task<IReadOnlyList<SlaClock>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets running or at-risk clocks that have passed their deadline but not yet marked as breached.
    /// </summary>
    Task<IReadOnlyList<SlaClock>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default);

    void Add(SlaClock clock);

    void Update(SlaClock clock);
}
