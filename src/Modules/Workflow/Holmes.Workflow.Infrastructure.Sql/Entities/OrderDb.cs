namespace Holmes.Workflow.Infrastructure.Sql.Entities;

public class OrderDb
{
    public string OrderId { get; set; } = null!;
    public string SubjectId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string PolicySnapshotId { get; set; } = null!;
    public string? PackageCode { get; set; }
    public string Status { get; set; } = null!;
    public string? BlockedFromStatus { get; set; }
    public string? LastStatusReason { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastUpdatedAt { get; set; }
    public string? ActiveIntakeSessionId { get; set; }
    public string? LastCompletedIntakeSessionId { get; set; }
    public DateTimeOffset? InvitedAt { get; set; }
    public DateTimeOffset? IntakeStartedAt { get; set; }
    public DateTimeOffset? IntakeCompletedAt { get; set; }
    public DateTimeOffset? ReadyForRoutingAt { get; set; }
    public DateTimeOffset? ClosedAt { get; set; }
    public DateTimeOffset? CanceledAt { get; set; }
}