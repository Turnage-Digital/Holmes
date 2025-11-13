using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Subjects.Domain.Events;

public sealed record SubjectMerged(
    UlidId SourceSubjectId,
    UlidId TargetSubjectId,
    UlidId MergedBy,
    DateTimeOffset MergedAt
) : INotification;