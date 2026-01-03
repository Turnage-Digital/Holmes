using Holmes.Core.Application;
using Holmes.Subjects.Contracts;
using Holmes.Subjects.Contracts.Dtos;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

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