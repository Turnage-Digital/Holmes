using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions.Commands;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class ResumeOrderFromBlockCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<ResumeOrderFromBlockCommand, Result>
{
    public async Task<Result> Handle(ResumeOrderFromBlockCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var order = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail($"Order '{request.OrderId}' not found.");
        }

        try
        {
            order.ResumeFromBlock(request.Reason, request.ResumedAt);
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