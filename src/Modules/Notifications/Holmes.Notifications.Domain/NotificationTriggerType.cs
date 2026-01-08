namespace Holmes.Notifications.Domain;

public enum NotificationTriggerType
{
    IntakeSessionInvited,
    IntakeSubmissionReceived,
    AuthorizationCaptured,
    OrderStateChanged,
    SlaClockAtRisk,
    SlaClockBreached,
    NotificationFailed
}
