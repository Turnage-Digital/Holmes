using Holmes.Core.Application;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class MarkOrderReadyForFulfillmentCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<MarkOrderReadyForFulfillmentCommand, Result>
{
    public async Task<Result> Handle(MarkOrderReadyForFulfillmentCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        try
        {
            order.MarkReadyForFulfillment(request.ReadyAt, request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}