using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Subjects.Contracts.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectReferencesQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectReferenceDto>>>;