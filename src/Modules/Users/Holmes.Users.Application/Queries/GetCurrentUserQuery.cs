using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Users.Contracts;
using Holmes.Users.Contracts.Dtos;
using MediatR;

namespace Holmes.Users.Application.Queries;

public sealed record GetCurrentUserQuery(
    string TargetUserId
) : RequestBase<Result<CurrentUserDto>>;

public sealed class GetCurrentUserQueryHandler(
    IUserQueries userQueries
) : IRequestHandler<GetCurrentUserQuery, Result<CurrentUserDto>>
{
    public async Task<Result<CurrentUserDto>> Handle(
        GetCurrentUserQuery request,
        CancellationToken cancellationToken
    )
    {
        var user = await userQueries.GetCurrentUserAsync(request.TargetUserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail<CurrentUserDto>($"User {request.TargetUserId} not found");
        }

        return Result.Success(user);
    }
}