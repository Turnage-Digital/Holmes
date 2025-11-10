using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed record SuspendCustomerCommand(
    UlidId TargetCustomerId,
    string Reason,
    DateTimeOffset SuspendedAt
) : RequestBase<Result>;

public sealed class SuspendCustomerCommandHandler(ICustomersUnitOfWork unitOfWork)
    : IRequestHandler<SuspendCustomerCommand, Result>
{
    public async Task<Result> Handle(SuspendCustomerCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Customers;
        var customer = await repository.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail($"Customer '{request.TargetCustomerId}' not found.");
        }

        var actor = request.GetUserUlid();
        customer.Suspend(request.Reason, actor, request.SuspendedAt);
        await repository.UpdateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}