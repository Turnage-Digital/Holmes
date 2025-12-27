using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions.Commands;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class CreateOrderCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, Result>
{
    public async Task<Result> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var existing = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            return Result.Fail($"Order '{request.OrderId}' already exists.");
        }

        var order = Order.Create(
            request.OrderId,
            request.SubjectId,
            request.CustomerId,
            request.PolicySnapshotId,
            request.CreatedAt,
            request.PackageCode);

        await repository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}