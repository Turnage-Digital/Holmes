using Holmes.Core.Application;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed class RenameCustomerCommandHandler(ICustomersUnitOfWork unitOfWork)
    : IRequestHandler<RenameCustomerCommand, Result>
{
    public async Task<Result> Handle(RenameCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await unitOfWork.Customers.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        customer.Rename(request.Name, request.RenamedAt);
        await unitOfWork.Customers.UpdateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}