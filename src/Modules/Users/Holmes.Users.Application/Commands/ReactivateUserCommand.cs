using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record ReactivateUserCommand(
    UlidId TargetUserId,
    DateTimeOffset ReactivatedAt
) : RequestBase<Result>;

public sealed class ReactivateUserCommandHandler(IUsersUnitOfWork unitOfWork)
    : IRequestHandler<ReactivateUserCommand, Result>
{
    public async Task<Result> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Users;
        var user = await repository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        var actor = request.GetUserUlid();
        user.Reactivate(actor, request.ReactivatedAt);
        await repository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
