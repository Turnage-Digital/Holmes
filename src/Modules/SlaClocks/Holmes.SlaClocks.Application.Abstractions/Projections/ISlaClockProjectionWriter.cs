using Holmes.SlaClocks.Domain;

namespace Holmes.SlaClocks.Application.Abstractions.Projections;

/// <summary>
///     Writes SLA clock projection data for read model queries.
///     Called by event handlers to keep projections in sync.
/// </summary>
public interface ISlaClockProjectionWriter
{
    /// <summary>
    ///     Inserts or updates a full SLA clock projection record.
    ///     Called on SlaClockStarted events.
    /// </summary>
    Task UpsertAsync(SlaClockProjectionModel model, CancellationToken cancellationToken);

    /// <summary>
    ///     Updates the clock state. Called on state transitions.
    /// </summary>
    Task UpdateStateAsync(string clockId, ClockState state, CancellationToken cancellationToken);

    /// <summary>
    ///     Updates pause information. Called on SlaClockPaused events.
    /// </summary>
    Task UpdatePauseInfoAsync(
        string clockId,
        ClockState state,
        DateTimeOffset pausedAt,
        string pauseReason,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates resume information. Called on SlaClockResumed events.
    /// </summary>
    Task UpdateResumeInfoAsync(
        string clockId,
        ClockState state,
        DateTimeOffset deadlineAt,
        DateTimeOffset atRiskThresholdAt,
        TimeSpan accumulatedPauseTime,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates at-risk information. Called on SlaClockAtRisk events.
    /// </summary>
    Task UpdateAtRiskAsync(
        string clockId,
        ClockState state,
        DateTimeOffset atRiskAt,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates breach information. Called on SlaClockBreached events.
    /// </summary>
    Task UpdateBreachedAsync(
        string clockId,
        ClockState state,
        DateTimeOffset breachedAt,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Updates completion information. Called on SlaClockCompleted events.
    /// </summary>
    Task UpdateCompletedAsync(
        string clockId,
        ClockState state,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken
    );
}

/// <summary>
///     Model representing the full SLA clock projection data.
/// </summary>
public sealed record SlaClockProjectionModel(
    string ClockId,
    string OrderId,
    string CustomerId,
    ClockKind Kind,
    ClockState State,
    DateTimeOffset StartedAt,
    DateTimeOffset DeadlineAt,
    DateTimeOffset AtRiskThresholdAt,
    int TargetBusinessDays,
    decimal AtRiskThresholdPercent
);