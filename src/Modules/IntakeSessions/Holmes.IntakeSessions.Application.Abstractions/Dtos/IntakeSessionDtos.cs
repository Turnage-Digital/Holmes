using Holmes.IntakeSessions.Domain;

namespace Holmes.IntakeSessions.Application.Abstractions.Dtos;

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
    ConsentArtifactDto? Consent,
    AnswersSnapshotDto? Answers,
    IntakeSectionConfigDto? SectionConfig
);

public sealed record ConsentArtifactDto(
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