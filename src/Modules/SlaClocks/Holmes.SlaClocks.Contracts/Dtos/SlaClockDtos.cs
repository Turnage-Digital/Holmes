using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;

namespace Holmes.SlaClocks.Contracts.Dtos;

public sealed record SlaClockDto(
    UlidId Id,
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    ClockState State,
    DateTimeOffset StartedAt,
    DateTimeOffset DeadlineAt,
    DateTimeOffset AtRiskThresholdAt,
    DateTimeOffset? AtRiskAt,
    DateTimeOffset? BreachedAt,
    DateTimeOffset? PausedAt,
    DateTimeOffset? CompletedAt,
    string? PauseReason,
    TimeSpan AccumulatedPauseTime,
    int TargetBusinessDays,
    decimal AtRiskThresholdPercent
);

public sealed record SlaClockSummaryDto(
    UlidId Id,
    ClockKind Kind,
    ClockState State,
    DateTimeOffset DeadlineAt,
    bool IsAtRisk,
    bool IsBreached
);