using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed class RegisterCustomerCommandHandler(ICustomersUnitOfWork unitOfWork)
    : IRequestHandler<RegisterCustomerCommand, UlidId>
{
    public async Task<UlidId> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var id = UlidId.NewUlid();
        var customer = Customer.Register(id, request.Name, request.RegisteredAt);
        await unitOfWork.Customers.AddAsync(customer, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return id;
    }
}