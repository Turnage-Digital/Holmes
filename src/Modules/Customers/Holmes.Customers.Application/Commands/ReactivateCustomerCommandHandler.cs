using Holmes.Core.Application;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed class ReactivateCustomerCommandHandler(ICustomersUnitOfWork unitOfWork)
    : IRequestHandler<ReactivateCustomerCommand, Result>
{
    public async Task<Result> Handle(ReactivateCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.Customers.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        var actor = request.GetUserUlid();
        customer.Reactivate(actor, request.ReactivatedAt);
        await unitOfWork.Customers.UpdateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}