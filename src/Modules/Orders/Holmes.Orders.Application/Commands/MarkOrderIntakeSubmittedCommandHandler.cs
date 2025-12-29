using Holmes.Core.Domain;
using Holmes.Orders.Application.Commands;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class MarkOrderIntakeSubmittedCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<MarkOrderIntakeSubmittedCommand, Result>
{
    public async Task<Result> Handle(MarkOrderIntakeSubmittedCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
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

        await repository.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}