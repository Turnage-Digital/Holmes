using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Contracts.IntegrationEvents;

public sealed record IntakeSessionInvitedIntegrationEvent(
    UlidId IntakeSessionId,
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string ResumeToken,
    DateTimeOffset InvitedAt,
    DateTimeOffset ExpiresAt,
    PolicySnapshot PolicySnapshot
) : INotification;