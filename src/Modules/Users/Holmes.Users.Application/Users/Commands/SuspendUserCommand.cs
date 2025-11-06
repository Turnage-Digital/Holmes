using System;
using System.Threading;
using System.Threading.Tasks;
using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain.Users;
using MediatR;

namespace Holmes.Users.Application.Users.Commands;

public sealed record SuspendUserCommand(
    UlidId TargetUserId,
    string Reason,
    UlidId PerformedBy,
    DateTimeOffset SuspendedAt
) : RequestBase<Result>;

public sealed class SuspendUserCommandHandler : IRequestHandler<SuspendUserCommand, Result>
{
    private readonly IUserRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public SuspendUserCommandHandler(IUserRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(SuspendUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _repository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user is null)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        user.Suspend(request.Reason, request.PerformedBy, request.SuspendedAt);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
