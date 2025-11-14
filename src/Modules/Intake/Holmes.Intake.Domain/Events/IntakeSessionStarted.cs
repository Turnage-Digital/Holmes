using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Intake.Domain.Events;

public sealed record IntakeSessionStarted(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset StartedAt,
    string? DeviceInfo
) : INotification;