using Holmes.Core.Application;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class CancelOrderCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<CancelOrderCommand, Result>
{
    public async Task<Result> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        try
        {
            order.Cancel(request.Reason, request.CanceledAt);
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        await unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}