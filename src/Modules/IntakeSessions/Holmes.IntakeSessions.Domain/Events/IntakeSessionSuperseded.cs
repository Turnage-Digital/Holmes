using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record IntakeSessionSuperseded(
    UlidId IntakeSessionId,
    UlidId OrderId,
    UlidId SupersededByIntakeSessionId,
    DateTimeOffset SupersededAt
) : INotification;