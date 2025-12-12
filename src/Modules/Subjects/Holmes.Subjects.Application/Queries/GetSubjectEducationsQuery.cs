using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectEducationsQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectEducationDto>>>;

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