using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Intake.Domain.Events;

public sealed record IntakeSessionSuperseded(
    UlidId IntakeSessionId,
    UlidId SupersededByIntakeSessionId,
    DateTimeOffset SupersededAt
) : INotification;