using Holmes.Core.Application;
using Holmes.Subjects.Contracts;
using Holmes.Subjects.Contracts.Dtos;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

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