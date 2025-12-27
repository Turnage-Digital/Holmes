using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed record RecordOrderInviteCommand(
    UlidId OrderId,
    UlidId IntakeSessionId,
    DateTimeOffset InvitedAt,
    string? Reason
) : RequestBase<Result>, ISkipUserAssignment;

public sealed class RecordOrderInviteCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<RecordOrderInviteCommand, Result>
{
    public async Task<Result> Handle(RecordOrderInviteCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail($"Order '{request.OrderId}' not found.");
        }

        try
        {
            order.RecordInvite(request.IntakeSessionId, request.InvitedAt, request.Reason);
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