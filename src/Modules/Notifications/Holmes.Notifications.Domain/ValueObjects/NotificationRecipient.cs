namespace Holmes.Notifications.Domain.ValueObjects;

public sealed record NotificationRecipient
{
    public NotificationChannel Channel { get; init; }
    public string Address { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public IReadOnlyDictionary<string, string> Metadata { get; init; } = new Dictionary<string, string>();

    public static NotificationRecipient Email(string emailAddress, string? displayName = null)
    {
        return new NotificationRecipient
        {
            Channel = NotificationChannel.Email,
            Address = emailAddress,
            DisplayName = displayName
        };
    }

    public static NotificationRecipient Sms(string phoneNumber, string? displayName = null)
    {
        return new NotificationRecipient
        {
            Channel = NotificationChannel.Sms,
            Address = phoneNumber,
            DisplayName = displayName
        };
    }

    public static NotificationRecipient Webhook(string url, IReadOnlyDictionary<string, string>? headers = null)
    {
        return new NotificationRecipient
        {
            Channel = NotificationChannel.Webhook,
            Address = url,
            Metadata = headers ?? new Dictionary<string, string>()
        };
    }
}