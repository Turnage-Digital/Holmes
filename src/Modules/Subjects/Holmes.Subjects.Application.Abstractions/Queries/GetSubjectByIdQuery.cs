using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;

namespace Holmes.Subjects.Application.Abstractions.Queries;

public sealed record GetSubjectByIdQuery(
    string SubjectId
) : RequestBase<Result<SubjectDetailDto>>;