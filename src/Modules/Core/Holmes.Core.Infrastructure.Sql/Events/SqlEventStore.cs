using Holmes.Core.Contracts.Events;
using Holmes.Core.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Core.Infrastructure.Sql.Events;

/// <summary>
///     SQL-backed event store implementation using MySQL/MariaDB.
/// </summary>
public sealed class SqlEventStore(CoreDbContext dbContext) : IEventStore
{
    public async Task AppendEventsAsync(
        string tenantId,
        string streamId,
        string streamType,
        IReadOnlyCollection<EventEnvelope> events,
        bool markAsDispatched,
        CancellationToken cancellationToken
    )
    {
        if (events.Count == 0)
        {
            return;
        }

        // Get the current max version for this stream
        var currentVersion = await dbContext.Events
            .Where(e => e.TenantId == tenantId && e.StreamId == streamId)
            .MaxAsync(e => (long?)e.Version, cancellationToken) ?? 0;

        var records = new List<EventRecord>(events.Count);
        var version = currentVersion;
        var now = DateTime.UtcNow;

        foreach (var envelope in events)
        {
            version++;
            var idempotencyKey = $"{streamId}:{version}:{envelope.EventId}";

            records.Add(new EventRecord
            {
                TenantId = tenantId,
                StreamId = streamId,
                StreamType = streamType,
                Version = version,
                EventId = envelope.EventId,
                Name = envelope.EventName,
                Payload = envelope.Payload,
                CorrelationId = envelope.CorrelationId,
                CausationId = envelope.CausationId,
                ActorId = envelope.ActorId,
                IdempotencyKey = idempotencyKey,
                CreatedAt = now,
                DispatchedAt = markAsDispatched ? now : null
            });
        }

        dbContext.Events.AddRange(records);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StoredEvent>> ReadStreamAsync(
        string tenantId,
        string streamId,
        long fromPosition,
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var records = await dbContext.Events
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId &&
                        e.StreamId == streamId &&
                        e.Position > fromPosition)
            .OrderBy(e => e.Position)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return records.Select(ToStoredEvent).ToList();
    }

    public async Task<IReadOnlyList<StoredEvent>> ReadByStreamTypeAsync(
        string tenantId,
        string streamType,
        long fromPosition,
        int batchSize,
        DateTime? asOfTimestamp,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.Events
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId &&
                        e.StreamType == streamType &&
                        e.Position > fromPosition);

        if (asOfTimestamp.HasValue)
        {
            query = query.Where(e => e.CreatedAt <= asOfTimestamp.Value);
        }

        var records = await query
            .OrderBy(e => e.Position)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return records.Select(ToStoredEvent).ToList();
    }

    public async Task<IReadOnlyList<StoredEvent>> ReadAllAsync(
        string tenantId,
        long fromPosition,
        int batchSize,
        DateTime? asOfTimestamp,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.Events
            .AsNoTracking()
            .Where(e => e.TenantId == tenantId && e.Position > fromPosition);

        if (asOfTimestamp.HasValue)
        {
            query = query.Where(e => e.CreatedAt <= asOfTimestamp.Value);
        }

        var records = await query
            .OrderBy(e => e.Position)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return records.Select(ToStoredEvent).ToList();
    }

    public async Task<IReadOnlyList<StoredEvent>> ReadUndispatchedAsync(
        int batchSize,
        CancellationToken cancellationToken
    )
    {
        var records = await dbContext.Events
            .Where(e => e.DispatchedAt == null)
            .OrderBy(e => e.Position)
            .Take(batchSize)
            .ToListAsync(cancellationToken);

        return records.Select(ToStoredEvent).ToList();
    }

    public async Task MarkDispatchedAsync(
        long position,
        CancellationToken cancellationToken
    )
    {
        await dbContext.Events
            .Where(e => e.Position == position)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(e => e.DispatchedAt, DateTime.UtcNow),
                cancellationToken);
    }

    public async Task MarkDispatchedBatchAsync(
        IEnumerable<long> positions,
        CancellationToken cancellationToken
    )
    {
        var positionList = positions.ToList();
        if (positionList.Count == 0)
        {
            return;
        }

        await dbContext.Events
            .Where(e => positionList.Contains(e.Position))
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(e => e.DispatchedAt, DateTime.UtcNow),
                cancellationToken);
    }

    private static StoredEvent ToStoredEvent(EventRecord record)
    {
        return new StoredEvent(
            record.Position,
            record.StreamId,
            record.StreamType,
            record.Version,
            record.EventId,
            record.Name,
            record.Payload,
            record.CreatedAt,
            record.CorrelationId,
            record.CausationId,
            record.ActorId);
    }
}