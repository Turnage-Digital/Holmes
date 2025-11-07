using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using Holmes.Users.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed record AssignCustomerAdminCommand(
    UlidId TargetCustomerId,
    UlidId TargetUserId,
    UlidId AssignedBy,
    DateTimeOffset AssignedAt
) : RequestBase<Result>;

public sealed class AssignCustomerAdminCommandHandler : IRequestHandler<AssignCustomerAdminCommand, Result>
{
    private readonly ICustomerRepository _repository;
    private readonly ICustomersUnitOfWork _unitOfWork;
    private readonly IUserDirectory _userDirectory;

    public AssignCustomerAdminCommandHandler(
        ICustomerRepository repository,
        ICustomersUnitOfWork unitOfWork,
        IUserDirectory userDirectory
    )
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
        _userDirectory = userDirectory;
    }

    public async Task<Result> Handle(AssignCustomerAdminCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail($"Customer '{request.TargetCustomerId}' not found.");
        }

        var userExists = await _userDirectory.ExistsAsync(request.TargetUserId, cancellationToken);

        if (!userExists)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        customer.AssignAdmin(request.TargetUserId, request.AssignedBy, request.AssignedAt);
        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}