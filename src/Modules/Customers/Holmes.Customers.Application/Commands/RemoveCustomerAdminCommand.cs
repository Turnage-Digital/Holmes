using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed record RemoveCustomerAdminCommand(
    UlidId TargetCustomerId,
    UlidId TargetUserId,
    UlidId RemovedBy,
    DateTimeOffset RemovedAt
) : RequestBase<Result>;

public sealed class RemoveCustomerAdminCommandHandler : IRequestHandler<RemoveCustomerAdminCommand, Result>
{
    private readonly ICustomerRepository _repository;
    private readonly ICustomersUnitOfWork _unitOfWork;

    public RemoveCustomerAdminCommandHandler(ICustomerRepository repository, ICustomersUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RemoveCustomerAdminCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail($"Customer '{request.TargetCustomerId}' not found.");
        }

        try
        {
            customer.RemoveAdmin(request.TargetUserId, request.RemovedBy, request.RemovedAt);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}