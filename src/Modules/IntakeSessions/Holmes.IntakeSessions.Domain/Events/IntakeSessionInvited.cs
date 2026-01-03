using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record IntakeSessionInvited(
    UlidId IntakeSessionId,
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string ResumeToken,
    DateTimeOffset InvitedAt,
    DateTimeOffset ExpiresAt,
    PolicySnapshot PolicySnapshot
) : INotification;