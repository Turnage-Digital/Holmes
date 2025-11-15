namespace Holmes.Intake.Infrastructure.Sql.Entities;

public sealed class IntakeSessionProjectionDb
{
    public string IntakeSessionId { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string SubjectId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string PolicySnapshotId { get; set; } = null!;
    public string PolicySnapshotSchemaVersion { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastTouchedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? SupersededBySessionId { get; set; }
}
