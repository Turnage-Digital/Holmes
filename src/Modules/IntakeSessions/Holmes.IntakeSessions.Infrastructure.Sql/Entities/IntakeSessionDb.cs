namespace Holmes.IntakeSessions.Infrastructure.Sql.Entities;

public class IntakeSessionDb
{
    public string IntakeSessionId { get; set; } = null!;
    public string OrderId { get; set; } = null!;
    public string SubjectId { get; set; } = null!;
    public string CustomerId { get; set; } = null!;
    public string Status { get; set; } = null!;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset LastTouchedAt { get; set; }
    public DateTimeOffset ExpiresAt { get; set; }
    public string ResumeToken { get; set; } = null!;
    public string PolicySnapshotJson { get; set; } = null!;
    public string? AnswersSchemaVersion { get; set; }
    public string? AnswersPayloadHash { get; set; }
    public string? AnswersPayloadCipherText { get; set; }
    public DateTimeOffset? AnswersUpdatedAt { get; set; }
    public string? AuthorizationArtifactId { get; set; }
    public string? AuthorizationMimeType { get; set; }
    public long? AuthorizationLength { get; set; }
    public string? AuthorizationHash { get; set; }
    public string? AuthorizationHashAlgorithm { get; set; }
    public string? AuthorizationSchemaVersion { get; set; }
    public DateTimeOffset? AuthorizationCapturedAt { get; set; }
    public DateTimeOffset? SubmittedAt { get; set; }
    public DateTimeOffset? AcceptedAt { get; set; }
    public string? CancellationReason { get; set; }
    public string? SupersededBySessionId { get; set; }
}
