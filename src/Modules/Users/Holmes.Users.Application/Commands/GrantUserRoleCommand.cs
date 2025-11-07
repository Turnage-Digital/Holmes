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
    UlidId GrantedBy,
    DateTimeOffset GrantedAt
) : RequestBase<Result>;

public sealed class GrantUserRoleCommandHandler : IRequestHandler<GrantUserRoleCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IUsersUnitOfWork _unitOfWork;

    public GrantUserRoleCommandHandler(IUserRepository repository, IUsersUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(GrantUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        user.GrantRole(request.Role, request.CustomerId, request.GrantedBy, request.GrantedAt);
        await _repository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}