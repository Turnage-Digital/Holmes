namespace Holmes.Notifications.Domain;

public enum NotificationTriggerType
{
    IntakeSessionInvited,
    IntakeSubmissionReceived,
    ConsentCaptured,
    OrderStateChanged,
    SlaClockAtRisk,
    SlaClockBreached,
    NotificationFailed
}