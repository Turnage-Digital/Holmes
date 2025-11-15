using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using MediatR;

namespace Holmes.Workflow.Application.Commands;

public sealed record BlockOrderCommand(
    UlidId OrderId,
    string Reason,
    DateTimeOffset BlockedAt
) : RequestBase<Result>;

public sealed class BlockOrderCommandHandler(IWorkflowUnitOfWork unitOfWork)
    : IRequestHandler<BlockOrderCommand, Result>
{
    public async Task<Result> Handle(BlockOrderCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail($"Order '{request.OrderId}' not found.");
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

        await repository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}