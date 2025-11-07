using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record ReactivateUserCommand(
    UlidId TargetUserId,
    UlidId PerformedBy,
    DateTimeOffset ReactivatedAt
) : RequestBase<Result>;

public sealed class ReactivateUserCommandHandler : IRequestHandler<ReactivateUserCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IUsersUnitOfWork _unitOfWork;

    public ReactivateUserCommandHandler(IUserRepository repository, IUsersUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReactivateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        user.Reactivate(request.PerformedBy, request.ReactivatedAt);
        await _repository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}