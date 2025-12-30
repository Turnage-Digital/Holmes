using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Contracts.IntegrationEvents;

public sealed record IntakeSubmissionAcceptedIntegrationEvent(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset AcceptedAt
) : INotification;