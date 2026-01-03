using Holmes.Core.Application;
using Holmes.Subjects.Contracts;
using Holmes.Subjects.Contracts.Dtos;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed class GetSubjectAddressesQueryHandler(
    ISubjectQueries subjectQueries
) : IRequestHandler<GetSubjectAddressesQuery, Result<IReadOnlyList<SubjectAddressDto>>>
{
    public async Task<Result<IReadOnlyList<SubjectAddressDto>>> Handle(
        GetSubjectAddressesQuery request,
        CancellationToken cancellationToken
    )
    {
        var addresses = await subjectQueries.GetAddressesAsync(request.SubjectId, cancellationToken);
        return Result.Success(addresses);
    }
}