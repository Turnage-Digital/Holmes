using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectReferencesQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectReferenceDto>>>;