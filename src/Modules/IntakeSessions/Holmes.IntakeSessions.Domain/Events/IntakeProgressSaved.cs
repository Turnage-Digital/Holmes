using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record IntakeProgressSaved(
    UlidId IntakeSessionId,
    UlidId OrderId,
    IntakeAnswersSnapshot AnswersSnapshot
) : INotification;