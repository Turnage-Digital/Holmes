using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Intake.Domain.Events;

public sealed record IntakeSubmissionReceived(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset SubmittedAt
) : INotification;