using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions.Commands;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class CancelOrderCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail($"Order '{request.OrderId}' not found.");
        }

        try
        {
            order.Cancel(request.Reason, request.CanceledAt);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result.Fail(ex.Message);
        }

        await repository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}