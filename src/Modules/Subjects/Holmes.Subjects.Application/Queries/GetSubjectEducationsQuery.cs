using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Subjects.Contracts.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectEducationsQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectEducationDto>>>;