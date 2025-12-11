using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectPhonesQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectPhoneDto>>>;

public sealed class GetSubjectPhonesQueryHandler(
    ISubjectQueries subjectQueries
) : IRequestHandler<GetSubjectPhonesQuery, Result<IReadOnlyList<SubjectPhoneDto>>>
{
    public async Task<Result<IReadOnlyList<SubjectPhoneDto>>> Handle(
        GetSubjectPhonesQuery request,
        CancellationToken cancellationToken
    )
    {
        var phones = await subjectQueries.GetPhonesAsync(request.SubjectId, cancellationToken);
        return Result.Success(phones);
    }
}