namespace Holmes.Workflow.Infrastructure.Sql.Entities;

public sealed class OrderTimelineEventProjectionDb
{
    public string EventId { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string EventType { get; set; } = null!;
    public string Source { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? MetadataJson { get; set; }
    public DateTimeOffset OccurredAt { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}