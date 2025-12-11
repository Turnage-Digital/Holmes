using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectByIdQuery(
    string SubjectId
) : RequestBase<Result<SubjectDetailDto>>;

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