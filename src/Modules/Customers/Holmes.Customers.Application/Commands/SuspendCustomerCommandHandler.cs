using Holmes.Core.Application;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed class SuspendCustomerCommandHandler(ICustomersUnitOfWork unitOfWork)
    : IRequestHandler<SuspendCustomerCommand, Result>
{
    public async Task<Result> Handle(SuspendCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.Customers.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        var actor = request.GetUserUlid();
        customer.Suspend(request.Reason, actor, request.SuspendedAt);
        await unitOfWork.Customers.UpdateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}