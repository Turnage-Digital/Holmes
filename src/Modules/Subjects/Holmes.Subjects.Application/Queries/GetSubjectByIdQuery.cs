using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectByIdQuery(
    string SubjectId
) : RequestBase<Result<SubjectDetailDto>>;