using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.ValueObjects;

namespace Holmes.Notifications.Application.Commands;

public sealed record CreateNotificationCommand(
    UlidId CustomerId,
    NotificationTrigger Trigger,
    NotificationRecipient Recipient,
    NotificationContent Content,
    NotificationSchedule? Schedule = null,
    NotificationPriority Priority = NotificationPriority.Normal,
    bool IsAdverseAction = false,
    string? CorrelationId = null
) : RequestBase<Result<CreateNotificationResult>>;

public sealed record CreateNotificationResult(
    UlidId NotificationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor
);