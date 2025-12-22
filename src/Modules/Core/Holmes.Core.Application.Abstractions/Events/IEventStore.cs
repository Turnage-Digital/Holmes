namespace Holmes.Core.Application.Abstractions.Events;

/// <summary>
///     Append-only event store for persisting domain events.
///     Events are organized by streams (one per aggregate instance).
/// </summary>
public interface IEventStore
{
    /// <summary>
    ///     Appends events to a stream. Events are persisted atomically.
    /// </summary>
    Task AppendEventsAsync(
        string tenantId,
        string streamId,
        string streamType,
        IReadOnlyCollection<EventEnvelope> events,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Reads events from a specific stream, starting from a position.
    /// </summary>
    Task<IReadOnlyList<StoredEvent>> ReadStreamAsync(
        string tenantId,
        string streamId,
        long fromPosition,
        int batchSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Reads events across all streams of a given type, ordered by global position.
    ///     Used by projection runners to replay events.
    /// </summary>
    Task<IReadOnlyList<StoredEvent>> ReadByStreamTypeAsync(
        string tenantId,
        string streamType,
        long fromPosition,
        int batchSize,
        DateTime? asOfTimestamp,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Reads all events across all streams, ordered by global position.
    ///     Used for global projections or audit.
    /// </summary>
    Task<IReadOnlyList<StoredEvent>> ReadAllAsync(
        string tenantId,
        long fromPosition,
        int batchSize,
        DateTime? asOfTimestamp,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Reads events that have not been dispatched yet (outbox pattern).
    ///     Returns events ordered by position for consistent processing.
    /// </summary>
    Task<IReadOnlyList<StoredEvent>> ReadUndispatchedAsync(
        int batchSize,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Marks an event as dispatched. Used by outbox processor after
    ///     successfully publishing the event via MediatR.
    /// </summary>
    Task MarkDispatchedAsync(
        long position,
        CancellationToken cancellationToken
    );

    /// <summary>
    ///     Marks multiple events as dispatched in a single operation.
    /// </summary>
    Task MarkDispatchedBatchAsync(
        IEnumerable<long> positions,
        CancellationToken cancellationToken
    );
}

/// <summary>
///     Envelope containing a domain event ready for persistence.
/// </summary>
public sealed record EventEnvelope(
    string EventId,
    string EventName,
    string Payload,
    string? CorrelationId,
    string? CausationId,
    string? ActorId
);

/// <summary>
///     A persisted event read from the store.
/// </summary>
public sealed record StoredEvent(
    long Position,
    string StreamId,
    string StreamType,
    long Version,
    string EventId,
    string EventName,
    string Payload,
    DateTime CreatedAt,
    string? CorrelationId,
    string? CausationId,
    string? ActorId
);