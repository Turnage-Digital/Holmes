using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Domain.Events;

public sealed record DisclosureViewed(
    UlidId IntakeSessionId,
    UlidId OrderId,
    DateTimeOffset ViewedAt
) : INotification;
