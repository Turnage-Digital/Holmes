using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectEmploymentsQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectEmploymentDto>>>;

public sealed class GetSubjectEmploymentsQueryHandler(
    ISubjectQueries subjectQueries
) : IRequestHandler<GetSubjectEmploymentsQuery, Result<IReadOnlyList<SubjectEmploymentDto>>>
{
    public async Task<Result<IReadOnlyList<SubjectEmploymentDto>>> Handle(
        GetSubjectEmploymentsQuery request,
        CancellationToken cancellationToken
    )
    {
        var employments = await subjectQueries.GetEmploymentsAsync(request.SubjectId, cancellationToken);
        return Result.Success(employments);
    }
}