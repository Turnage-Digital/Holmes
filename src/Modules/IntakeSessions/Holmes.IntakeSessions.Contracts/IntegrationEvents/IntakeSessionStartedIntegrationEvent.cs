using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Contracts.IntegrationEvents;

public sealed record IntakeSessionStartedIntegrationEvent(
    UlidId OrderId,
    UlidId IntakeSessionId,
    DateTimeOffset OccurredAt
) : INotification;
