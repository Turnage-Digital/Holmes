using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record RevokeUserRoleCommand(
    UlidId TargetUserId,
    UserRole Role,
    string? CustomerId,
    DateTimeOffset RevokedAt
) : RequestBase<Result>;

public sealed class RevokeUserRoleCommandHandler(IUsersUnitOfWork unitOfWork)
    : IRequestHandler<RevokeUserRoleCommand, Result>
{
    public async Task<Result> Handle(RevokeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Users;
        var user = await repository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        var actor = request.GetUserUlid();
        try
        {
            user.RevokeRole(request.Role, request.CustomerId, actor, request.RevokedAt);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await repository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}