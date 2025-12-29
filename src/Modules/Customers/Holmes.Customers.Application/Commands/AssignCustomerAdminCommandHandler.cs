using Holmes.Core.Domain;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Domain;
using Holmes.Users.Application.Abstractions;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed class AssignCustomerAdminCommandHandler(
    ICustomersUnitOfWork unitOfWork,
    IUserDirectory userDirectory
) : IRequestHandler<AssignCustomerAdminCommand, Result>
{
    public async Task<Result> Handle(AssignCustomerAdminCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Customers;
        var customer = await repository.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail($"Customer '{request.TargetCustomerId}' not found.");
        }

        var userExists = await userDirectory.ExistsAsync(request.TargetUserId, cancellationToken);

        if (!userExists)
        {
            return Result.Fail($"User '{request.TargetUserId}' not found.");
        }

        var actor = request.GetUserUlid();
        customer.AssignAdmin(request.TargetUserId, actor, request.AssignedAt);
        await repository.UpdateAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}