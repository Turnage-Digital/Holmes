using Holmes.Core.Application;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class CreateOrderCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, Result>
{
    public async Task<Result> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
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
            request.PackageCode,
            request.CreatedBy);

        await unitOfWork.Orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
