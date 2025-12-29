using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Application.Commands;

public sealed record RequestSubjectIntakeCommand(
    string SubjectEmail,
    string? SubjectPhone,
    UlidId CustomerId,
    string PolicySnapshotId,
    DateTimeOffset RequestedAt
) : RequestBase<Result<RequestSubjectIntakeResult>>;

public sealed record RequestSubjectIntakeResult(
    UlidId SubjectId,
    bool SubjectWasExisting,
    UlidId OrderId
);