using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Abstractions.Queries;
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