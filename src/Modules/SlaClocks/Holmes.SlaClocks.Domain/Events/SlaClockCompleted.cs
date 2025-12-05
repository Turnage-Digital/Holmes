using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.SlaClocks.Domain.Events;

public sealed record SlaClockCompleted(
    UlidId ClockId,
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    DateTimeOffset CompletedAt,
    DateTimeOffset DeadlineAt,
    bool WasAtRisk,
    TimeSpan TotalElapsed
) : INotification;
