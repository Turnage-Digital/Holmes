namespace Holmes.Workflow.Infrastructure.Sql.Entities;

public class OrderSummaryProjectionDb
{
    public string OrderId { get; set; } = null!;
    public string SubjectId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string PolicySnapshotId { get; set; } = null!;
    public string? PackageCode { get; set; }
    public string Status { get; set; } = null!;
    public string? LastStatusReason { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? ReadyForFulfillmentAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset? CanceledAt { get; set; }
}