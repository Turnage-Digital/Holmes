using System.Text.Json;
using Holmes.Workflow.Application.Abstractions.Projections;
using Holmes.Workflow.Infrastructure.Sql.Entities;
using Microsoft.Extensions.Logging;

namespace Holmes.Workflow.Infrastructure.Sql.Projections;

public sealed class SqlOrderTimelineWriter(
    WorkflowDbContext dbContext,
    ILogger<SqlOrderTimelineWriter> logger
) : IOrderTimelineWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task WriteAsync(OrderTimelineEntry entry, CancellationToken cancellationToken)
    {
        var entity = new OrderTimelineEventProjectionDb
        {
            EventId = Ulid.NewUlid().ToString(),
            OrderId = entry.OrderId.ToString(),
            EventType = entry.EventType,
            Description = entry.Description,
            Source = entry.Source,
            OccurredAt = entry.OccurredAt,
            RecordedAt = DateTimeOffset.UtcNow,
            MetadataJson = entry.Metadata is null ? null : JsonSerializer.Serialize(entry.Metadata, SerializerOptions)
        };

        dbContext.OrderTimelineEvents.Add(entity);
        await dbContext.SaveChangesAsync(cancellationToken);

        logger.LogDebug("Recorded timeline event {EventType} for Order {OrderId}", entry.EventType, entry.OrderId);
        OrderTimelineMetrics.EventsWritten.Add(1,
            KeyValuePair.Create<string, object?>("event", entry.EventType));
    }
}