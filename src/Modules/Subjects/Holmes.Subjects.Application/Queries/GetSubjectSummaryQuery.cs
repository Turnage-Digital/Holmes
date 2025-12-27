using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed class GetSubjectSummaryQueryHandler(
    ISubjectQueries subjectQueries
) : IRequestHandler<GetSubjectSummaryQuery, Result<SubjectSummaryDto>>
{
    public async Task<Result<SubjectSummaryDto>> Handle(
        GetSubjectSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var subject = await subjectQueries.GetSummaryByIdAsync(request.SubjectId, cancellationToken);

        if (subject is null)
        {
            return Result.Fail<SubjectSummaryDto>($"Subject {request.SubjectId} not found");
        }

        return Result.Success(subject);
    }
}
