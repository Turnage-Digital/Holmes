using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using MediatR;

namespace Holmes.Customers.Application.Commands;

public sealed record RegisterCustomerCommand(
    string Name,
    DateTimeOffset RegisteredAt
) : RequestBase<UlidId>;

public sealed class RegisterCustomerCommandHandler : IRequestHandler<RegisterCustomerCommand, UlidId>
{
    private readonly ICustomerRepository _repository;
    private readonly ICustomersUnitOfWork _unitOfWork;

    public RegisterCustomerCommandHandler(ICustomerRepository repository, ICustomersUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UlidId> Handle(RegisterCustomerCommand request, CancellationToken cancellationToken)
    {
        var id = UlidId.NewUlid();
        var customer = Customer.Register(id, request.Name, request.RegisteredAt);
        await _repository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return id;
    }
}