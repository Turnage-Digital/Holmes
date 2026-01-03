using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Contracts.IntegrationEvents;

public sealed record SlaClockAtRiskIntegrationEvent(
    UlidId ClockId,
    UlidId OrderId,
    UlidId CustomerId,
    ClockKind Kind,
    DateTimeOffset AtRiskAt,
    DateTimeOffset DeadlineAt
) : INotification;