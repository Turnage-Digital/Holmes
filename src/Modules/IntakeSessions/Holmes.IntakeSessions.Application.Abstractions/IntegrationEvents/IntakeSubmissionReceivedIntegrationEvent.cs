using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Application.Abstractions.IntegrationEvents;

public sealed record IntakeSubmissionReceivedIntegrationEvent(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset SubmittedAt
) : INotification;
