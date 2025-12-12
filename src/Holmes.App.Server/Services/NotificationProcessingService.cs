using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Application.Abstractions.Queries;
using Holmes.Notifications.Application.Commands;
using MediatR;

namespace Holmes.App.Server.Services;

public sealed class NotificationProcessingService(
    IServiceScopeFactory scopeFactory,
    ILogger<NotificationProcessingService> logger
) : BackgroundService
{
    private const int BatchSize = 50;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(5);

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
        var notificationQueries = scope.ServiceProvider.GetRequiredService<INotificationQueries>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var pending = await notificationQueries.GetPendingAsync(BatchSize, cancellationToken);

        if (pending.Count is 0)
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
                    new ProcessNotificationCommand(UlidId.Parse(notification.Id)),
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