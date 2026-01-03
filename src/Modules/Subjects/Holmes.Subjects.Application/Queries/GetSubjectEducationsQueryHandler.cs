using Holmes.Core.Application;
using Holmes.Subjects.Contracts;
using Holmes.Subjects.Contracts.Dtos;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed class GetSubjectEducationsQueryHandler(
    ISubjectQueries subjectQueries
) : IRequestHandler<GetSubjectEducationsQuery, Result<IReadOnlyList<SubjectEducationDto>>>
{
    public async Task<Result<IReadOnlyList<SubjectEducationDto>>> Handle(
        GetSubjectEducationsQuery request,
        CancellationToken cancellationToken
    )
    {
        var educations = await subjectQueries.GetEducationsAsync(request.SubjectId, cancellationToken);
        return Result.Success(educations);
    }
}