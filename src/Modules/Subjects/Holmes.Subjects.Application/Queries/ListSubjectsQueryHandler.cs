using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed class ListSubjectsQueryHandler(
    ISubjectQueries subjectQueries
) : IRequestHandler<ListSubjectsQuery, Result<SubjectPagedResult>>
{
    public async Task<Result<SubjectPagedResult>> Handle(
        ListSubjectsQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await subjectQueries.GetSubjectsPagedAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}