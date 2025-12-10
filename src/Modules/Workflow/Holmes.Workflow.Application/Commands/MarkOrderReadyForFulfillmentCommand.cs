using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using MediatR;

namespace Holmes.Workflow.Application.Commands;

public sealed record MarkOrderReadyForFulfillmentCommand(
    UlidId OrderId,
    DateTimeOffset ReadyAt,
    string? Reason
) : RequestBase<Result>;

public sealed class MarkOrderReadyForFulfillmentCommandHandler(IWorkflowUnitOfWork unitOfWork)
    : IRequestHandler<MarkOrderReadyForFulfillmentCommand, Result>
{
    public async Task<Result> Handle(MarkOrderReadyForFulfillmentCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail($"Order '{request.OrderId}' not found.");
        }

        try
        {
            order.MarkReadyForFulfillment(request.ReadyAt, request.Reason);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await repository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}