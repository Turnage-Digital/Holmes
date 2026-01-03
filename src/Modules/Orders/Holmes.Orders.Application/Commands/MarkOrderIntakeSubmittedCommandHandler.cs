using Holmes.Core.Application;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class MarkOrderIntakeSubmittedCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<MarkOrderIntakeSubmittedCommand, Result>
{
    public async Task<Result> Handle(MarkOrderIntakeSubmittedCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail($"Order '{request.OrderId}' not found.");
        }

        try
        {
            order.MarkIntakeSubmitted(request.IntakeSessionId, request.SubmittedAt, request.Reason);
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