using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed record ReactivateCustomerCommand(
    UlidId TargetCustomerId,
    UlidId PerformedBy,
    DateTimeOffset ReactivatedAt
) : RequestBase<Result>;

public sealed class ReactivateCustomerCommandHandler : IRequestHandler<ReactivateCustomerCommand, Result>
{
    private readonly ICustomerRepository _repository;
    private readonly ICustomersUnitOfWork _unitOfWork;

    public ReactivateCustomerCommandHandler(ICustomerRepository repository, ICustomersUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(ReactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail($"Customer '{request.TargetCustomerId}' not found.");
        }

        customer.Reactivate(request.PerformedBy, request.ReactivatedAt);
        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}