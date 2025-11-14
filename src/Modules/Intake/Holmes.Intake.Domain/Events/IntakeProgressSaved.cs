using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain.ValueObjects;
using MediatR;

namespace Holmes.Intake.Domain.Events;

public sealed record IntakeProgressSaved(
    UlidId IntakeSessionId,
    UlidId OrderId,
    IntakeAnswersSnapshot AnswersSnapshot
) : INotification;