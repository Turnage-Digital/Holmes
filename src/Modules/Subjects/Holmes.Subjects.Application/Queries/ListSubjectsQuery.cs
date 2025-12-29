using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Abstractions.Dtos;

namespace Holmes.Subjects.Application.Queries;

public sealed record ListSubjectsQuery(
    int Page,
    int PageSize
) : RequestBase<Result<SubjectPagedResult>>;
