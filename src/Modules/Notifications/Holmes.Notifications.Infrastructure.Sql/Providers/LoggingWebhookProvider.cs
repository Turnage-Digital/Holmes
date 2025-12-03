using System.Text.Json;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.Storage;
using Holmes.Notifications.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace Holmes.Notifications.Infrastructure.Sql.Providers;

/// <summary>
/// Stub webhook provider that logs instead of posting.
/// Replace with HttpWebhookProvider when ready.
/// </summary>
public sealed class LoggingWebhookProvider(ILogger<LoggingWebhookProvider> logger) : INotificationProvider
{
    public NotificationChannel Channel => NotificationChannel.Webhook;

    public bool CanHandle(NotificationChannel channel) => channel == NotificationChannel.Webhook;

    public Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default)
    {
        var payload = new
        {
            Subject = content.Subject,
            Body = content.Body,
            TemplateId = content.TemplateId,
            TemplateData = content.TemplateData
        };

        // TODO: Replace with actual HTTP POST
        logger.LogDebug(
            "[STUB WEBHOOK] POST {Url}, Headers: {Headers}, Payload: {Payload}",
            recipient.Address,
            JsonSerializer.Serialize(recipient.Metadata),
            JsonSerializer.Serialize(payload));

        // Simulate success
        var fakeMessageId = $"stub-webhook-{Guid.NewGuid():N}";
        return Task.FromResult(NotificationSendResult.Succeeded(fakeMessageId));
    }
}
