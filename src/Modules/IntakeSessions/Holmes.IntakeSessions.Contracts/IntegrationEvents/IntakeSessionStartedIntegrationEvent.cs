using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Contracts.IntegrationEvents;

public sealed record IntakeSessionStartedIntegrationEvent(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset StartedAt,
    string? DeviceInfo
) : INotification;