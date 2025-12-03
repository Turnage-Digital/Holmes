using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Notifications.Domain.Events;

public sealed record NotificationBounced(
    UlidId NotificationId,
    UlidId CustomerId,
    UlidId? OrderId,
    DateTimeOffset BouncedAt,
    string Reason
) : INotification;