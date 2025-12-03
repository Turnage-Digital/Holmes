using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Notifications.Domain.Events;

public sealed record NotificationDeliveryFailed(
    UlidId NotificationId,
    UlidId CustomerId,
    UlidId? OrderId,
    NotificationChannel Channel,
    DateTimeOffset FailedAt,
    string Reason,
    int AttemptNumber
) : INotification;
