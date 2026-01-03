using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record IntakeSessionExpired(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset ExpiredAt,
    string Reason
) : INotification;