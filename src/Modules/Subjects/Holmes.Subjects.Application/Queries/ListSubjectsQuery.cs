using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions;

namespace Holmes.Subjects.Application.Queries;

public sealed record ListSubjectsQuery(
    int Page,
    int PageSize
) : RequestBase<Result<SubjectPagedResult>>;