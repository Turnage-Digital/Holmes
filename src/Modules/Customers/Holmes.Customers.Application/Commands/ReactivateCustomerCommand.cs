using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed record ReactivateCustomerCommand(
    UlidId TargetCustomerId,
    DateTimeOffset ReactivatedAt
) : RequestBase<Result>;

public sealed class ReactivateCustomerCommandHandler(ICustomersUnitOfWork unitOfWork)
    : IRequestHandler<ReactivateCustomerCommand, Result>
{
    public async Task<Result> Handle(ReactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Customers;
        var customer = await repository.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail($"Customer '{request.TargetCustomerId}' not found.");
        }

        var actor = request.GetUserUlid();
        customer.Reactivate(actor, request.ReactivatedAt);
        await repository.UpdateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}