namespace Holmes.Notifications.Domain;

public enum DeliveryStatus
{
    Pending,
    Queued,
    Sending,
    Delivered,
    Failed,
    Bounced,
    Cancelled
}
