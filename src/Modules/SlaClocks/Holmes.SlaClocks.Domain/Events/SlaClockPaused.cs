using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.SlaClocks.Domain.Events;

public sealed record SlaClockPaused(
    UlidId ClockId,
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    string Reason,
    DateTimeOffset PausedAt
) : INotification;
