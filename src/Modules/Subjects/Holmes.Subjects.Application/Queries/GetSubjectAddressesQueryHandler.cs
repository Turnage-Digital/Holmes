using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Queries;
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