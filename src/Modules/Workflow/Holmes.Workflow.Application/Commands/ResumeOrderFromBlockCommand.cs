using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using MediatR;

namespace Holmes.Workflow.Application.Commands;

public sealed record ResumeOrderFromBlockCommand(
    UlidId OrderId,
    string Reason,
    DateTimeOffset ResumedAt
) : RequestBase<Result>;

public sealed class ResumeOrderFromBlockCommandHandler(IWorkflowUnitOfWork unitOfWork)
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