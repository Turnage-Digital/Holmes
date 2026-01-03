using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Application.Commands;

public sealed record CreateSubjectCommand(
    string SubjectEmail,
    string? SubjectPhone,
    DateTimeOffset RequestedAt
) : RequestBase<Result<CreateSubjectResult>>;

public sealed record CreateSubjectResult(
    UlidId SubjectId,
    bool SubjectWasExisting
);
