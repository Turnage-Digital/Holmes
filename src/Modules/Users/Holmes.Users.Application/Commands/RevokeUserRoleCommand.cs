using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Users.Application.Commands;

public sealed record RevokeUserRoleCommand(
    UlidId TargetUserId,
    UserRole Role,
    string? CustomerId,
    UlidId RevokedBy,
    DateTimeOffset RevokedAt
) : RequestBase<Result>;

public sealed class RevokeUserRoleCommandHandler : IRequestHandler<RevokeUserRoleCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IUsersUnitOfWork _unitOfWork;

    public RevokeUserRoleCommandHandler(IUserRepository repository, IUsersUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RevokeUserRoleCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        try
        {
            user.RevokeRole(request.Role, request.CustomerId, request.RevokedBy, request.RevokedAt);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _repository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}