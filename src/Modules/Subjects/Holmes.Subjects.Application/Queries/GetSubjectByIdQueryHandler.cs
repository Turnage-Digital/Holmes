using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed class GetSubjectByIdQueryHandler(
    ISubjectQueries subjectQueries
) : IRequestHandler<GetSubjectByIdQuery, Result<SubjectDetailDto>>
{
    public async Task<Result<SubjectDetailDto>> Handle(
        GetSubjectByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var subject = await subjectQueries.GetDetailByIdAsync(request.SubjectId, cancellationToken);

        if (subject is null)
        {
            return Result.Fail<SubjectDetailDto>($"Subject {request.SubjectId} not found");
        }

        return Result.Success(subject);
    }
}