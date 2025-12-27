using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record IntakeSessionStarted(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset StartedAt,
    string? DeviceInfo
) : INotification;