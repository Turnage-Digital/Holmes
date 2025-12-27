using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record IntakeSubmissionReceived(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset SubmittedAt
) : INotification;