using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

/// <summary>
///     Transitions an order from FulfillmentInProgress to ReadyForReport.
///     Called when all required Services for the order have completed.
/// </summary>
public sealed record MarkOrderReadyForReportCommand(
    UlidId OrderId,
    DateTimeOffset ReadyAt,
    string? Reason
) : RequestBase<Result>, ISkipUserAssignment;

public sealed class MarkOrderReadyForReportCommandHandler(IWorkflowUnitOfWork unitOfWork)
    : IRequestHandler<MarkOrderReadyForReportCommand, Result>
{
    public async Task<Result> Handle(MarkOrderReadyForReportCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail($"Order '{request.OrderId}' not found.");
        }

        try
        {
            order.MarkReadyForReport(request.ReadyAt, request.Reason);
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