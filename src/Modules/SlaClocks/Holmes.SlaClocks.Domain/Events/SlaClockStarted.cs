using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.SlaClocks.Domain.Events;

public sealed record SlaClockStarted(
    UlidId ClockId,
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    DateTimeOffset StartedAt,
    DateTimeOffset DeadlineAt,
    DateTimeOffset AtRiskThresholdAt,
    int TargetBusinessDays
) : INotification;
