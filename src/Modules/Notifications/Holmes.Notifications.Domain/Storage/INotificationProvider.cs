using Holmes.Notifications.Domain.ValueObjects;

namespace Holmes.Notifications.Domain.Storage;

public interface INotificationProvider
{
    NotificationChannel Channel { get; }

    Task<NotificationSendResult> SendAsync(
        NotificationRecipient recipient,
        NotificationContent content,
        CancellationToken cancellationToken = default
    );

    bool CanHandle(NotificationChannel channel);
}

public sealed record NotificationSendResult
{
    public bool Success { get; init; }
    public string? ProviderMessageId { get; init; }
    public string? ErrorMessage { get; init; }
    public bool ShouldRetry { get; init; }
    public TimeSpan? RetryAfter { get; init; }

    public static NotificationSendResult Succeeded(string? providerMessageId = null)
    {
        return new NotificationSendResult
        {
            Success = true,
            ProviderMessageId = providerMessageId
        };
    }

    public static NotificationSendResult Failed(
        string errorMessage,
        bool shouldRetry = true,
        TimeSpan? retryAfter = null
    )
    {
        return new NotificationSendResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ShouldRetry = shouldRetry,
            RetryAfter = retryAfter
        };
    }

    public static NotificationSendResult PermanentFailure(string errorMessage)
    {
        return new NotificationSendResult
        {
            Success = false,
            ErrorMessage = errorMessage,
            ShouldRetry = false
        };
    }
}