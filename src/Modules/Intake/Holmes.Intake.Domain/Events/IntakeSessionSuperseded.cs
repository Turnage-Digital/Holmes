using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Intake.Domain.Events;

public sealed record IntakeSessionSuperseded(
    UlidId IntakeSessionId,
    UlidId OrderId,
    UlidId SupersededByIntakeSessionId,
    DateTimeOffset SupersededAt
) : INotification;
