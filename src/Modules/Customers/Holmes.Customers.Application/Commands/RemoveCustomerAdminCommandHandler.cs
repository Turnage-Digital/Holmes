using Holmes.Core.Domain;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed class RemoveCustomerAdminCommandHandler(ICustomersUnitOfWork unitOfWork)
    : IRequestHandler<RemoveCustomerAdminCommand, Result>
{
    public async Task<Result> Handle(RemoveCustomerAdminCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Customers;
        var customer = await repository.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail($"Customer '{request.TargetCustomerId}' not found.");
        }

        var actor = request.GetUserUlid();
        try
        {
            customer.RemoveAdmin(request.TargetUserId, actor, request.RemovedAt);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await repository.UpdateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}