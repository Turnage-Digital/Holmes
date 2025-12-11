using Holmes.Intake.Domain;

namespace Holmes.Intake.Application.Abstractions.Dtos;

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
    AnswersSnapshotDto? Answers
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