using Holmes.Core.Application;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class BlockOrderCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<BlockOrderCommand, Result>
{
    public async Task<Result> Handle(BlockOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        try
        {
            order.Block(request.Reason, request.BlockedAt);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }
        catch (ArgumentException ex)
        {
            return Result.Fail(ex.Message);
        }

        await unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}