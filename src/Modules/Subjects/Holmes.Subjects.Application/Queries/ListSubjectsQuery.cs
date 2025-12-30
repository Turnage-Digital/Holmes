using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Subjects.Contracts;

namespace Holmes.Subjects.Application.Queries;

public sealed record ListSubjectsQuery(
    int Page,
    int PageSize
) : RequestBase<Result<SubjectPagedResult>>;