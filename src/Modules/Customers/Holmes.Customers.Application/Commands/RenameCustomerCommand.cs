using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed record RenameCustomerCommand(
    UlidId TargetCustomerId,
    string Name,
    DateTimeOffset RenamedAt
) : RequestBase<Result>;

public sealed class RenameCustomerCommandHandler(ICustomersUnitOfWork unitOfWork)
    : IRequestHandler<RenameCustomerCommand, Result>
{
    public async Task<Result> Handle(RenameCustomerCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Customers;
        var customer = await repository.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail($"Customer '{request.TargetCustomerId}' not found.");
        }

        customer.Rename(request.Name, request.RenamedAt);
        await repository.UpdateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}