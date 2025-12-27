using Holmes.Core.Application.Abstractions.Events;
using MediatR;

namespace Holmes.App.Server.Services;

/// <summary>
///     Background service that processes deferred domain event dispatch.
///     When SaveChangesAsync is called with deferDispatch: true, events are
///     persisted to the event store but not immediately dispatched via MediatR.
///     This processor polls for undispatched events and publishes them,
///     ensuring events are only dispatched after their transaction commits.
/// </summary>
public sealed class DeferredDispatchProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<DeferredDispatchProcessor> logger
) : BackgroundService
{
    private const int BatchSize = 100;
    private static readonly TimeSpan PollingInterval = TimeSpan.FromMilliseconds(100);
    private static readonly TimeSpan IdleInterval = TimeSpan.FromSeconds(1);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("DeferredDispatchProcessor started");

        while (!stoppingToken.IsCancellationRequested)
        {
            var processedCount = 0;

            try
            {
                processedCount = await ProcessDeferredEventsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "DeferredDispatchProcessor encountered an error");
            }

            try
            {
                // Poll more frequently when there's work to do
                var delay = processedCount > 0 ? PollingInterval : IdleInterval;
                await Task.Delay(delay, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // Shutdown requested
            }
        }

        logger.LogInformation("DeferredDispatchProcessor stopped");
    }

    private async Task<int> ProcessDeferredEventsAsync(CancellationToken cancellationToken)
    {
        using var scope = scopeFactory.CreateScope();
        var eventStore = scope.ServiceProvider.GetRequiredService<IEventStore>();
        var serializer = scope.ServiceProvider.GetRequiredService<IDomainEventSerializer>();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var pending = await eventStore.ReadUndispatchedAsync(BatchSize, cancellationToken);

        if (pending.Count == 0)
        {
            return 0;
        }

        logger.LogDebug("Processing {Count} deferred event(s)", pending.Count);

        var successfulPositions = new List<long>();

        foreach (var storedEvent in pending)
        {
            try
            {
                var domainEvent = serializer.Deserialize(storedEvent.Payload, storedEvent.EventName);

                await mediator.Publish(domainEvent, cancellationToken);

                successfulPositions.Add(storedEvent.Position);

                logger.LogTrace(
                    "Dispatched event {EventName} at position {Position}",
                    storedEvent.EventName,
                    storedEvent.Position);
            }
            catch (Exception ex)
            {
                // Log but continue - don't let one bad event block the whole outbox
                // The event will be retried on the next poll
                logger.LogError(
                    ex,
                    "Failed to dispatch event {EventName} at position {Position}",
                    storedEvent.EventName,
                    storedEvent.Position);
            }
        }

        // Mark successfully dispatched events
        if (successfulPositions.Count > 0)
        {
            await eventStore.MarkDispatchedBatchAsync(successfulPositions, cancellationToken);

            logger.LogDebug(
                "Marked {Count} event(s) as dispatched",
                successfulPositions.Count);
        }

        return successfulPositions.Count;
    }
}