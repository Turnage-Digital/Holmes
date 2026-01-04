using Holmes.Core.Application;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class RecordOrderInviteCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<RecordOrderInviteCommand, Result>
{
    public async Task<Result> Handle(RecordOrderInviteCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        try
        {
            order.RecordInvite(request.IntakeSessionId, request.InvitedAt, request.Reason);
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