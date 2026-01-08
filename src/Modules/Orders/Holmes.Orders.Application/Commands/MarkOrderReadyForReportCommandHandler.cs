using Holmes.Core.Application;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class MarkOrderReadyForReportCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<MarkOrderReadyForReportCommand, Result>
{
    public async Task<Result> Handle(MarkOrderReadyForReportCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        try
        {
            order.MarkReadyForReport(request.ReadyAt, request.Reason);
        }
        catch (InvalidOperationException)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        await unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}