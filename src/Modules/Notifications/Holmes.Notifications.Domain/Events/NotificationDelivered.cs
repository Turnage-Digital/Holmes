using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Notifications.Domain.Events;

public sealed record NotificationDelivered(
    UlidId NotificationId,
    UlidId CustomerId,
    UlidId? OrderId,
    NotificationChannel Channel,
    DateTimeOffset DeliveredAt,
    string? ProviderMessageId
) : INotification;
