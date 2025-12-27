using Holmes.Core.Application;
using Holmes.Core.Domain;

namespace Holmes.Subjects.Application.Abstractions.Queries;

public sealed record ListSubjectsQuery(
    int Page,
    int PageSize
) : RequestBase<Result<SubjectPagedResult>>;