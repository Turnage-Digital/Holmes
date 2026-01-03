using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Contracts;
using Holmes.Users.Contracts.Dtos;
using MediatR;

namespace Holmes.Users.Application.Queries;

public sealed record GetUserByIdQuery(
    UlidId TargetUserId
) : RequestBase<Result<UserDto>>;

public sealed class GetUserByIdQueryHandler(
    IUserQueries userQueries
) : IRequestHandler<GetUserByIdQuery, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var user = await userQueries.GetByIdAsync(request.TargetUserId, cancellationToken);

        if (user is null)
        {
            return Result.Fail<UserDto>($"User {request.TargetUserId} not found");
        }

        return Result.Success(user);
    }
}