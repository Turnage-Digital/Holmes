namespace Holmes.App.Server.Contracts;

public sealed record CaptureConsentArtifactRequest(
    string MimeType,
    string SchemaVersion,
    string PayloadBase64,
    DateTimeOffset? CapturedAt,
    IReadOnlyDictionary<string, string>? Metadata
);

public sealed record SubmitIntakeRequest(DateTimeOffset? SubmittedAt);

public sealed record AcceptIntakeSubmissionRequest(DateTimeOffset? AcceptedAt);
