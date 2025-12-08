using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Holmes.Notifications.Infrastructure.Sql;

/// <summary>
///     Stub SMS provider that logs instead of sending.
///     Replace with TwilioSmsProvider when ready.
/// </summary>
public sealed class LoggingSmsProvider(ILogger<LoggingSmsProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Sms;

    public bool CanHandle(NotificationChannel channel)
    {
        return channel == NotificationChannel.Sms;
    }

    public Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default
    )
    {
        // TODO: Replace with actual Twilio implementation
        logger.LogDebug(
            "[STUB SMS] To: {To}, Body: {Body}",
            recipient.Address,
            content.Body.Length > 160 ? content.Body[..160] + "..." : content.Body);

        // Simulate success
        var fakeMessageId = $"stub-sms-{Guid.NewGuid():N}";
        return Task.FromResult(NotificationSendResult.Succeeded(fakeMessageId));
    }
}