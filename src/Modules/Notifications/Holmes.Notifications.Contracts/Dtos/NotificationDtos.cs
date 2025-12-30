using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain;

namespace Holmes.Notifications.Contracts.Dtos;

public sealed record NotificationSummaryDto(
    UlidId Id,
    UlidId CustomerId,
    UlidId? OrderId,
    NotificationTriggerType TriggerType,
    NotificationChannel Channel,
    string RecipientAddress,
    DeliveryStatus Status,
    bool IsAdverseAction,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeliveredAt,
    int DeliveryAttemptCount
);