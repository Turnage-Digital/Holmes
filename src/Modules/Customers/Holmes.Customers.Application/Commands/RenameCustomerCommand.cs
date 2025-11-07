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

public sealed class RenameCustomerCommandHandler : IRequestHandler<RenameCustomerCommand, Result>
{
    private readonly ICustomerRepository _repository;
    private readonly ICustomersUnitOfWork _unitOfWork;

    public RenameCustomerCommandHandler(ICustomerRepository repository, ICustomersUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result> Handle(RenameCustomerCommand request, CancellationToken cancellationToken)
    {
        var customer = await _repository.GetByIdAsync(request.TargetCustomerId, cancellationToken);
        if (customer is null)
        {
            return Result.Fail($"Customer '{request.TargetCustomerId}' not found.");
        }

        customer.Rename(request.Name, request.RenamedAt);
        await _repository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}