using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record IntakeSubmissionAccepted(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset AcceptedAt
) : INotification;