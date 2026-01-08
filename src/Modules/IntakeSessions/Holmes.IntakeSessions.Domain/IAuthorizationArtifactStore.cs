using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Domain;

public interface IAuthorizationArtifactStore
{
    Task<AuthorizationArtifactDescriptor> SaveAsync(
        AuthorizationArtifactWriteRequest request,
        Stream payload,
        CancellationToken cancellationToken
    );

    Task<AuthorizationArtifactStream?> GetAsync(UlidId artifactId, CancellationToken cancellationToken);

    Task<bool> ExistsAsync(UlidId artifactId, CancellationToken cancellationToken);
}

public sealed record AuthorizationArtifactWriteRequest(
    UlidId ArtifactId,
    UlidId OrderId,
    UlidId SubjectId,
    string MimeType,
    string SchemaVersion,
    DateTimeOffset CapturedAt,
    IReadOnlyDictionary<string, string> Metadata
);

public sealed record AuthorizationArtifactDescriptor(
    UlidId ArtifactId,
    string MimeType,
    long Length,
    string Hash,
    string HashAlgorithm,
    string SchemaVersion,
    DateTimeOffset CreatedAt,
    string? StorageHint = null
);

public sealed record AuthorizationArtifactStream(
    AuthorizationArtifactDescriptor Descriptor,
    Stream Content
);
