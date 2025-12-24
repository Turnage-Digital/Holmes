using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

/// <summary>
///     Transitions an order from ReadyForFulfillment to FulfillmentInProgress.
///     Called after Services have been created for the order.
/// </summary>
public sealed record BeginOrderFulfillmentCommand(
    UlidId OrderId,
    DateTimeOffset StartedAt,
    string? Reason
) : RequestBase<Result>, ISkipUserAssignment;

public sealed class BeginOrderFulfillmentCommandHandler(IWorkflowUnitOfWork unitOfWork)
    : IRequestHandler<BeginOrderFulfillmentCommand, Result>
{
    public async Task<Result> Handle(BeginOrderFulfillmentCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail($"Order '{request.OrderId}' not found.");
        }

        try
        {
            order.BeginFulfillment(request.StartedAt, request.Reason);
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