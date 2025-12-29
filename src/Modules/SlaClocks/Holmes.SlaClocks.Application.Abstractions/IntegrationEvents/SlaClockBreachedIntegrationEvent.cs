using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Abstractions.IntegrationEvents;

public sealed record SlaClockBreachedIntegrationEvent(
    UlidId ClockId,
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    DateTimeOffset BreachedAt,
    DateTimeOffset DeadlineAt
) : INotification;