using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record UpdateUserProfileCommand(
    UlidId TargetUserId,
    string Email,
    string? DisplayName,
    DateTimeOffset UpdatedAt
) : RequestBase<Result>;

public sealed class UpdateUserProfileCommandHandler(IUsersUnitOfWork unitOfWork)
    : IRequestHandler<UpdateUserProfileCommand, Result>
{
    public async Task<Result> Handle(UpdateUserProfileCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Users;
        var user = await repository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        user.UpdateProfile(request.Email, request.DisplayName, request.UpdatedAt);
        await repository.UpdateAsync(user, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}