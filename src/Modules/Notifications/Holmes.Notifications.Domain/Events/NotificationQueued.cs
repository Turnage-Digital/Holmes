using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Notifications.Domain.Events;

public sealed record NotificationQueued(
    UlidId NotificationId,
    DateTimeOffset QueuedAt
) : INotification;