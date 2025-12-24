namespace Holmes.IntakeSessions.Domain.ValueObjects;

public sealed record PolicySnapshot(
    string SnapshotId,
    string SchemaVersion,
    DateTimeOffset CapturedAt,
    IReadOnlyDictionary<string, string> Metadata
)
{
    public static PolicySnapshot Create(
        string snapshotId,
        string schemaVersion,
        DateTimeOffset capturedAt,
        IReadOnlyDictionary<string, string>? metadata = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(snapshotId);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);
        metadata ??= new Dictionary<string, string>();
        return new PolicySnapshot(snapshotId, schemaVersion, capturedAt, metadata);
    }
}