using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Users.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Users.Application.Queries;

public sealed record ListUsersQuery(
    int Page,
    int PageSize
) : RequestBase<Result<UserPagedResult>>;

public sealed class ListUsersQueryHandler(
    IUserQueries userQueries
) : IRequestHandler<ListUsersQuery, Result<UserPagedResult>>
{
    public async Task<Result<UserPagedResult>> Handle(
        ListUsersQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await userQueries.GetUsersPagedAsync(
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}