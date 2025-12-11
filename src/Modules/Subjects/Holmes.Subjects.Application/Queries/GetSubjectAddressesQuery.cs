using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Subjects.Application.Abstractions.Dtos;
using Holmes.Subjects.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Subjects.Application.Queries;

public sealed record GetSubjectAddressesQuery(
    string SubjectId
) : RequestBase<Result<IReadOnlyList<SubjectAddressDto>>>;

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