using Holmes.Core.Application;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed class RemoveCustomerAdminCommandHandler(ICustomersUnitOfWork unitOfWork)
    : IRequestHandler<RemoveCustomerAdminCommand, Result>
{
    public async Task<Result> Handle(RemoveCustomerAdminCommand request, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.Customers.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        var actor = request.GetUserUlid();
        try
        {
            customer.RemoveAdmin(request.TargetUserId, actor, request.RemovedAt);
        }
        catch (InvalidOperationException)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        await unitOfWork.Customers.UpdateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}