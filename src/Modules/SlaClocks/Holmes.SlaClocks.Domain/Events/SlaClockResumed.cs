using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.SlaClocks.Domain.Events;

public sealed record SlaClockResumed(
    UlidId ClockId,
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    DateTimeOffset ResumedAt,
    TimeSpan PauseDuration
) : INotification;
