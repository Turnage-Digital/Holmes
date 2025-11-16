using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using MediatR;

namespace Holmes.Workflow.Application.Commands;

public sealed record CreateOrderCommand(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId CustomerId,
    string PolicySnapshotId,
    DateTimeOffset CreatedAt,
    string? PackageCode
) : RequestBase<Result>;

public sealed class CreateOrderCommandHandler(IWorkflowUnitOfWork unitOfWork)
    : IRequestHandler<CreateOrderCommand, Result>
{
    public async Task<Result> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.Orders;
        var existing = await repository.GetByIdAsync(request.OrderId, cancellationToken);
        if (existing is not null)
        {
            return Result.Fail($"Order '{request.OrderId}' already exists.");
        }

        var order = Order.Create(
            request.OrderId,
            request.SubjectId,
            request.CustomerId,
            request.PolicySnapshotId,
            request.CreatedAt,
            request.PackageCode);

        await repository.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}