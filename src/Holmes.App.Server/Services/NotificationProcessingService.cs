using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Domain;
using MediatR;

namespace Holmes.App.Server.Services;

public sealed class NotificationProcessingService(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationProcessingService> logger
) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);
    private const int BatchSize = 50;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("NotificationProcessingService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingNotificationsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "NotificationProcessingService encountered an error");
            }

            try
            {
                await Task.Delay(PollingInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested
            }
        }

        logger.LogInformation("NotificationProcessingService stopped");
    }

    private async Task ProcessPendingNotificationsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<INotificationsUnitOfWork>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var pending = await unitOfWork.NotificationRequests.GetPendingAsync(BatchSize, cancellationToken);

        if (pending.Count == 0)
        {
            logger.LogTrace("No pending notifications to process");
            return;
        }

        logger.LogInformation(
            "Processing {Count} pending notification(s)",
            pending.Count);

        foreach (var notification in pending)
        {
            try
            {
                var result = await sender.Send(
                    new ProcessNotificationCommand(notification.Id),
                    cancellationToken);

                if (result.IsSuccess)
                {
                    logger.LogDebug(
                        "Notification {NotificationId} processed successfully",
                        notification.Id);
                }
                else
                {
                    logger.LogWarning(
                        "Notification {NotificationId} processing failed: {Error}",
                        notification.Id,
                        result.Error);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Exception processing notification {NotificationId}",
                    notification.Id);
            }
        }
    }
}
