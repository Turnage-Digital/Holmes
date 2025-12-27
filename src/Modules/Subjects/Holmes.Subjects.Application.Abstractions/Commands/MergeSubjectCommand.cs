using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Subjects.Application.Abstractions.Commands;

public sealed record MergeSubjectCommand(
    UlidId SourceSubjectId,
    UlidId TargetSubjectId,
    DateTimeOffset MergedAt
) : RequestBase<Result>;