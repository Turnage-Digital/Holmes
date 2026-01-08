using Holmes.Core.Application;
using Holmes.Orders.Domain;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class ResumeOrderFromBlockCommandHandler(IOrdersUnitOfWork unitOfWork)
    : IRequestHandler<ResumeOrderFromBlockCommand, Result>
{
    public async Task<Result> Handle(ResumeOrderFromBlockCommand request, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            return Result.Fail(ResultErrors.NotFound);
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

        await unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}