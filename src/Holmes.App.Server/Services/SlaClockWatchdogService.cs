using Holmes.Core.Domain;
using Holmes.SlaClocks.Application.Commands;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.App.Server.Services;

public sealed class SlaClockWatchdogService(
    IServiceScopeFactory scopeFactory,
    ILogger<SlaClockWatchdogService> logger
) : BackgroundService
{
    private static readonly TimeSpan CheckInterval = TimeSpan.FromMinutes(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("SlaClockWatchdogService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CheckClocksAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "SlaClockWatchdogService encountered an error");
            }

            try
            {
                await Task.Delay(CheckInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested
            }
        }

        logger.LogInformation("SlaClockWatchdogService stopped");
    }

    private async Task CheckClocksAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<ISlaClockRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var now = DateTimeOffset.UtcNow;

        // Find running clocks past at-risk threshold (not yet marked)
        var atRiskClocks = await repository.GetRunningClocksPastThresholdAsync(now, cancellationToken);
        if (atRiskClocks.Count > 0)
        {
            logger.LogInformation("Found {Count} clocks past at-risk threshold", atRiskClocks.Count);
        }

        foreach (var clock in atRiskClocks)
        {
            try
            {
                var command = new MarkClockAtRiskCommand(clock.Id, now)
                {
                    UserId = SystemActors.SlaClockWatchdog
                };
                await sender.Send(command, cancellationToken);
                logger.LogInformation(
                    "Marked clock {ClockId} as at-risk for order {OrderId}",
                    clock.Id, clock.OrderId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to mark clock {ClockId} as at-risk", clock.Id);
            }
        }

        // Find running/at-risk clocks past deadline (not yet breached)
        var breachedClocks = await repository.GetRunningClocksPastDeadlineAsync(now, cancellationToken);
        if (breachedClocks.Count > 0)
        {
            logger.LogWarning("Found {Count} clocks past deadline", breachedClocks.Count);
        }

        foreach (var clock in breachedClocks)
        {
            try
            {
                var command = new MarkClockBreachedCommand(clock.Id, now)
                {
                    UserId = SystemActors.SlaClockWatchdog
                };
                await sender.Send(command, cancellationToken);
                logger.LogWarning(
                    "Marked clock {ClockId} as breached for order {OrderId}",
                    clock.Id, clock.OrderId);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to mark clock {ClockId} as breached", clock.Id);
            }
        }
    }
}