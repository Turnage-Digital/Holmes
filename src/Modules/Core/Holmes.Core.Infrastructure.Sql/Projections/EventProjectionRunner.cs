using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql.Entities;
using Holmes.Core.Infrastructure.Sql.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Core.Infrastructure.Sql.Projections;

/// <summary>
/// Base class for event-based projection runners. Reads events from the event store
/// and dispatches them through MediatR handlers to rebuild projections.
/// </summary>
public abstract class EventProjectionRunner
{
    private readonly CoreDbContext _coreDbContext;
    private readonly IEventStore _eventStore;
    private readonly IDomainEventSerializer _serializer;
    private readonly IPublisher _publisher;
    private readonly ILogger _logger;

    protected EventProjectionRunner(
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger logger)
    {
        _coreDbContext = coreDbContext;
        _eventStore = eventStore;
        _serializer = serializer;
        _publisher = publisher;
        _logger = logger;
    }

    /// <summary>
    /// Unique name for this projection, used for checkpoint storage.
    /// </summary>
    protected abstract string ProjectionName { get; }

    /// <summary>
    /// Stream types to process (e.g., "Order", "User"). Null means all streams.
    /// </summary>
    protected abstract string[]? StreamTypes { get; }

    /// <summary>
    /// Resets the projection state (truncates tables) before replay.
    /// </summary>
    protected abstract Task ResetProjectionAsync(CancellationToken cancellationToken);

    public async Task<ProjectionReplayResult> RunAsync(
        bool reset,
        int batchSize,
        CancellationToken cancellationToken)
    {
        if (reset)
        {
            _logger.LogInformation("Resetting projection {ProjectionName}...", ProjectionName);
            await ResetProjectionAsync(cancellationToken);
            await DeleteCheckpointAsync(cancellationToken);
        }

        var lastPosition = await LoadCheckpointAsync(cancellationToken);
        var processed = 0;
        var currentPosition = lastPosition;
        DateTime? lastEventTime = null;

        _logger.LogInformation(
            "Starting event replay for {ProjectionName} from position {Position}",
            ProjectionName,
            lastPosition);

        while (true)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var events = await LoadEventBatchAsync(currentPosition, batchSize, cancellationToken);
            if (events.Count == 0)
            {
                break;
            }

            foreach (var storedEvent in events)
            {
                cancellationToken.ThrowIfCancellationRequested();

                try
                {
                    var notification = _serializer.Deserialize(storedEvent.Payload, storedEvent.EventName);
                    await _publisher.Publish(notification, cancellationToken);
                    processed++;
                    currentPosition = storedEvent.Position;
                    lastEventTime = storedEvent.CreatedAt;
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Failed to process event {EventId} at position {Position} for projection {ProjectionName}",
                        storedEvent.EventId,
                        storedEvent.Position,
                        ProjectionName);
                    throw;
                }
            }

            await SaveCheckpointAsync(currentPosition, cancellationToken);

            _logger.LogDebug(
                "Processed batch of {Count} events, position now at {Position}",
                events.Count,
                currentPosition);
        }

        _logger.LogInformation(
            "Event replay complete for {ProjectionName}. Processed {Count} events, final position {Position}",
            ProjectionName,
            processed,
            currentPosition);

        return new ProjectionReplayResult(
            processed,
            lastEventTime.HasValue ? new DateTimeOffset(lastEventTime.Value, TimeSpan.Zero) : null,
            currentPosition > 0 ? currentPosition.ToString() : null);
    }

    private async Task<IReadOnlyList<StoredEvent>> LoadEventBatchAsync(
        long fromPosition,
        int batchSize,
        CancellationToken cancellationToken)
    {
        // Use "*" tenant for now - process all tenants
        const string tenantId = "*";

        if (StreamTypes is null || StreamTypes.Length == 0)
        {
            return await _eventStore.ReadAllAsync(tenantId, fromPosition, batchSize, null, cancellationToken);
        }

        // If filtering by stream types, we need to merge results from each type
        // For simplicity, query each type and merge by position
        var allEvents = new List<StoredEvent>();

        foreach (var streamType in StreamTypes)
        {
            var events = await _eventStore.ReadByStreamTypeAsync(
                tenantId,
                streamType,
                fromPosition,
                batchSize,
                null,
                cancellationToken);
            allEvents.AddRange(events);
        }

        // Sort by position and take batch size
        return allEvents
            .OrderBy(e => e.Position)
            .Take(batchSize)
            .ToList();
    }

    private async Task<long> LoadCheckpointAsync(CancellationToken cancellationToken)
    {
        var checkpoint = await _coreDbContext.ProjectionCheckpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(
                x => x.ProjectionName == ProjectionName && x.TenantId == "*",
                cancellationToken);

        return checkpoint?.Position ?? 0;
    }

    private async Task SaveCheckpointAsync(long position, CancellationToken cancellationToken)
    {
        var checkpoint = await _coreDbContext.ProjectionCheckpoints
            .FirstOrDefaultAsync(
                x => x.ProjectionName == ProjectionName && x.TenantId == "*",
                cancellationToken);

        if (checkpoint is null)
        {
            checkpoint = new ProjectionCheckpoint
            {
                ProjectionName = ProjectionName,
                TenantId = "*"
            };
            _coreDbContext.ProjectionCheckpoints.Add(checkpoint);
        }

        checkpoint.Position = position;
        checkpoint.UpdatedAt = DateTime.UtcNow;

        await _coreDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task DeleteCheckpointAsync(CancellationToken cancellationToken)
    {
        var checkpoint = await _coreDbContext.ProjectionCheckpoints
            .FirstOrDefaultAsync(
                x => x.ProjectionName == ProjectionName && x.TenantId == "*",
                cancellationToken);

        if (checkpoint is not null)
        {
            _coreDbContext.ProjectionCheckpoints.Remove(checkpoint);
            await _coreDbContext.SaveChangesAsync(cancellationToken);
        }
    }
}
