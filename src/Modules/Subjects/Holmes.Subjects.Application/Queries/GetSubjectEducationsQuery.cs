using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Subjects.Contracts.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectEducationsQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectEducationDto>>>;