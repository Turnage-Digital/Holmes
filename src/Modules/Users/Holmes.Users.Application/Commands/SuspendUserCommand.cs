using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record SuspendUserCommand(
    UlidId TargetUserId,
    string Reason,
    DateTimeOffset SuspendedAt
) : RequestBase<Result>;

public sealed class SuspendUserCommandHandler(IUsersUnitOfWork unitOfWork)
    : IRequestHandler<SuspendUserCommand, Result>
{
    public async Task<Result> Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Users;
        var user = await repository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        var actor = request.GetUserUlid();
        user.Suspend(request.Reason, actor, request.SuspendedAt);
        await repository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}