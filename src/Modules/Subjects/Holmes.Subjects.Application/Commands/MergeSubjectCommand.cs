using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Application.Commands;

public sealed record MergeSubjectCommand(
    UlidId SourceSubjectId,
    UlidId TargetSubjectId,
    DateTimeOffset MergedAt
) : RequestBase<Result>;