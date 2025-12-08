using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.SlaClocks.Domain.Events;

public sealed record SlaClockBreached(
    UlidId ClockId,
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    DateTimeOffset BreachedAt,
    DateTimeOffset DeadlineAt
) : INotification;