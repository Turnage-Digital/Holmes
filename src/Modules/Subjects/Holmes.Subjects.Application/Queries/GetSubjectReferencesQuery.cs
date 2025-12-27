using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

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
