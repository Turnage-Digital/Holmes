using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Notifications.Domain.ValueObjects;

public sealed record NotificationTrigger
{
    public NotificationTriggerType Type { get; init; }
    public UlidId? OrderId { get; init; }
    public UlidId? SubjectId { get; init; }
    public UlidId? CustomerId { get; init; }
    public string? FromState { get; init; }
    public string? ToState { get; init; }
    public IReadOnlyDictionary<string, object> Context { get; init; } = new Dictionary<string, object>();

    public static NotificationTrigger IntakeInvited(UlidId orderId, UlidId subjectId, UlidId customerId)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.IntakeSessionInvited,
            OrderId = orderId,
            SubjectId = subjectId,
            CustomerId = customerId
        };
    }

    public static NotificationTrigger OrderStateChanged(
        UlidId orderId,
        UlidId customerId,
        string fromState,
        string toState
    )
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.OrderStateChanged,
            OrderId = orderId,
            CustomerId = customerId,
            FromState = fromState,
            ToState = toState
        };
    }

    public static NotificationTrigger SlaAtRisk(UlidId orderId, UlidId customerId, string clockKind)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.SlaClockAtRisk,
            OrderId = orderId,
            CustomerId = customerId,
            Context = new Dictionary<string, object> { ["ClockKind"] = clockKind }
        };
    }

    public static NotificationTrigger SlaBreached(UlidId orderId, UlidId customerId, string clockKind)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.SlaClockBreached,
            OrderId = orderId,
            CustomerId = customerId,
            Context = new Dictionary<string, object> { ["ClockKind"] = clockKind }
        };
    }

    public static NotificationTrigger DeliveryFailed(UlidId notificationId, UlidId customerId, string reason)
    {
        return new NotificationTrigger
        {
            Type = NotificationTriggerType.NotificationFailed,
            CustomerId = customerId,
            Context = new Dictionary<string, object>
            {
                ["FailedNotificationId"] = notificationId.ToString(),
                ["FailureReason"] = reason
            }
        };
    }
}