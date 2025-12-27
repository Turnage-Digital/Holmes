using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Notifications.Domain.Events;

public sealed record NotificationCreated(
    UlidId NotificationId,
    UlidId CustomerId,
    UlidId? OrderId,
    UlidId? SubjectId,
    NotificationTriggerType TriggerType,
    NotificationChannel Channel,
    bool IsAdverseAction,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor
) : INotification;