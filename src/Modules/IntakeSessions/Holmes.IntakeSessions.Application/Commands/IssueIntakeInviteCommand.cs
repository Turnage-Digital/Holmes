using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed record IssueIntakeInviteCommand(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    string PolicySnapshotSchemaVersion,
    IReadOnlyDictionary<string, string> PolicyMetadata,
    IReadOnlyList<string>? OrderedServiceCodes,
    DateTimeOffset PolicyCapturedAt,
    DateTimeOffset InvitedAt,
    TimeSpan TimeToLive,
    string? ResumeToken
) : RequestBase<Result<IssueIntakeInviteResult>>;

public sealed record IssueIntakeInviteResult(
    UlidId IntakeSessionId,
    string ResumeToken,
    DateTimeOffset ExpiresAt
);