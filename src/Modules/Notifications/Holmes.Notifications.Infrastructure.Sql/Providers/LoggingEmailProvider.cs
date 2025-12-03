using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.Storage;
using Holmes.Notifications.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Holmes.Notifications.Infrastructure.Sql.Providers;

/// <summary>
/// Stub email provider that logs instead of sending.
/// Replace with SendGridEmailProvider when ready.
/// </summary>
public sealed class LoggingEmailProvider(ILogger<LoggingEmailProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Email;

    public bool CanHandle(NotificationChannel channel) => channel == NotificationChannel.Email;

    public Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default)
    {
        // TODO: Replace with actual SendGrid implementation
        logger.LogDebug(
            "[STUB EMAIL] To: {To} ({DisplayName}), Subject: {Subject}, Body: {Body}, TemplateId: {TemplateId}",
            recipient.Address,
            recipient.DisplayName,
            content.Subject,
            content.Body.Length > 100 ? content.Body[..100] + "..." : content.Body,
            content.TemplateId);

        // Simulate success
        var fakeMessageId = $"stub-email-{Guid.NewGuid():N}";
        return Task.FromResult(NotificationSendResult.Succeeded(fakeMessageId));
    }
}
