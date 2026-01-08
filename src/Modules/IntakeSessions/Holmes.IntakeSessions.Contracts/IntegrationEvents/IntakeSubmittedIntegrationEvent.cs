using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Contracts.IntegrationEvents;

public sealed record IntakeSubmittedIntegrationEvent(
    UlidId OrderId,
    UlidId IntakeSessionId,
    DateTimeOffset OccurredAt
) : INotification;
