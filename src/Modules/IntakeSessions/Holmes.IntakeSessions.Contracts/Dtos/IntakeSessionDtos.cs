using Holmes.IntakeSessions.Domain;

namespace Holmes.IntakeSessions.Contracts.Dtos;

public sealed record IntakeSessionSummaryDto(
    string Id,
    string OrderId,
    string SubjectId,
    string CustomerId,
    string PolicySnapshotId,
    string PolicySnapshotSchemaVersion,
    IntakeSessionStatus Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastTouchedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? AcceptedAt,
    string? CancellationReason,
    string? SupersededBySessionId
);

public sealed record IntakeSessionBootstrapDto(
    string SessionId,
    string OrderId,
    string SubjectId,
    string CustomerId,
    string PolicySnapshotId,
    string PolicySnapshotSchemaVersion,
    string Status,
    DateTimeOffset ExpiresAt,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastTouchedAt,
    DateTimeOffset? SubmittedAt,
    DateTimeOffset? AcceptedAt,
    string? CancellationReason,
    string? SupersededBySessionId,
    DisclosureSnapshotDto? Disclosure,
    AuthorizationCopyDto? AuthorizationCopy,
    string AuthorizationMode,
    AuthorizationArtifactDto? Authorization,
    AnswersSnapshotDto? Answers,
    IntakeSectionConfigDto? SectionConfig
);

public sealed record DisclosureSnapshotDto(
    string DisclosureId,
    string DisclosureVersion,
    string DisclosureHash,
    string DisclosureFormat,
    string? DisclosureContent,
    string? DisclosureFetchUrl
);

public sealed record AuthorizationCopyDto(
    string AuthorizationId,
    string AuthorizationVersion,
    string AuthorizationHash,
    string AuthorizationFormat,
    string? AuthorizationContent
);

public sealed record AuthorizationArtifactDto(
    string ArtifactId,
    string MimeType,
    long Length,
    string Hash,
    string HashAlgorithm,
    string SchemaVersion,
    DateTimeOffset CapturedAt
);

public sealed record AnswersSnapshotDto(
    string SchemaVersion,
    string PayloadHash,
    string PayloadCipherText,
    DateTimeOffset UpdatedAt
);

/// <summary>
///     Configuration for which intake form sections should be displayed,
///     based on the services ordered for this background check.
/// </summary>
public sealed record IntakeSectionConfigDto(
    IReadOnlyList<string> RequiredSections,
    IReadOnlyList<string> EnabledServiceCodes
);
