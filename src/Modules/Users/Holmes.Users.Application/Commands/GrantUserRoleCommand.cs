using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record GrantUserRoleCommand(
    UlidId TargetUserId,
    UserRole Role,
    string? CustomerId,
    DateTimeOffset GrantedAt
) : RequestBase<Result>;

public sealed class GrantUserRoleCommandHandler(IUsersUnitOfWork unitOfWork)
    : IRequestHandler<GrantUserRoleCommand, Result>
{
    public async Task<Result> Handle(GrantUserRoleCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Users;
        var user = await repository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        var actor = request.GetUserUlid();
        user.GrantRole(request.Role, request.CustomerId, actor, request.GrantedAt);
        await repository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}