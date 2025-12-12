using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Intake.Domain;

public interface IConsentArtifactStore
{
    Task<ConsentArtifactDescriptor> SaveAsync(
        ConsentArtifactWriteRequest request,
        Stream payload,
        CancellationToken cancellationToken
    );

    Task<ConsentArtifactStream?> GetAsync(UlidId artifactId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(UlidId artifactId, CancellationToken cancellationToken);
}

public sealed record ConsentArtifactWriteRequest(
    UlidId ArtifactId,
    UlidId OrderId,
    UlidId SubjectId,
    string MimeType,
    string SchemaVersion,
    DateTimeOffset CapturedAt,
    IReadOnlyDictionary<string, string> Metadata
);

public sealed record ConsentArtifactDescriptor(
    UlidId ArtifactId,
    string MimeType,
    long Length,
    string Hash,
    string HashAlgorithm,
    string SchemaVersion,
    DateTimeOffset CreatedAt,
    string? StorageHint = null
);

public sealed record ConsentArtifactStream(
    ConsentArtifactDescriptor Descriptor,
    Stream Content
);