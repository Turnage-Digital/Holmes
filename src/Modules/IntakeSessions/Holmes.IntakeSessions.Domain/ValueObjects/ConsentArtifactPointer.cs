using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Domain.ValueObjects;

public sealed record ConsentArtifactPointer(
    UlidId ArtifactId,
    string MimeType,
    long Length,
    string Hash,
    string HashAlgorithm,
    string SchemaVersion,
    DateTimeOffset CapturedAt
)
{
    public static ConsentArtifactPointer Create(
        UlidId artifactId,
        string mimeType,
        long length,
        string hash,
        string hashAlgorithm,
        string schemaVersion,
        DateTimeOffset capturedAt
    )
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(length);

        ArgumentException.ThrowIfNullOrWhiteSpace(mimeType);
        ArgumentException.ThrowIfNullOrWhiteSpace(hash);
        ArgumentException.ThrowIfNullOrWhiteSpace(hashAlgorithm);
        ArgumentException.ThrowIfNullOrWhiteSpace(schemaVersion);

        return new ConsentArtifactPointer(artifactId, mimeType, length, hash, hashAlgorithm, schemaVersion, capturedAt);
    }
}