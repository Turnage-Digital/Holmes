using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Abstractions.Dtos;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectReferencesQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectReferenceDto>>>;

public sealed class GetSubjectReferencesQueryHandler(
    ISubjectQueries subjectQueries
) : IRequestHandler<GetSubjectReferencesQuery, Result<IReadOnlyList<SubjectReferenceDto>>>
{
    public async Task<Result<IReadOnlyList<SubjectReferenceDto>>> Handle(
        GetSubjectReferencesQuery request,
        CancellationToken cancellationToken
    )
    {
        var references = await subjectQueries.GetReferencesAsync(request.SubjectId, cancellationToken);
        return Result.Success(references);
    }
}