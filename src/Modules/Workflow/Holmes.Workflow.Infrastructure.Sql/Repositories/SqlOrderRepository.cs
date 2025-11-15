using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Workflow.Infrastructure.Sql.Repositories;

public sealed class SqlOrderRepository(WorkflowDbContext dbContext) : IOrderRepository
{
    public Task AddAsync(Order order, CancellationToken cancellationToken)
    {
        dbContext.Orders.Add(ToEntity(order));
        return Task.CompletedTask;
    }

    public async Task<Order?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var record = await dbContext.Orders
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.OrderId == id.ToString(), cancellationToken);

        return record is null ? null : Rehydrate(record);
    }

    public async Task UpdateAsync(Order order, CancellationToken cancellationToken)
    {
        var record = await dbContext.Orders
            .FirstOrDefaultAsync(x => x.OrderId == order.Id.ToString(), cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException($"Order '{order.Id}' not found.");
        }

        ApplyState(order, record);
    }

    private static Order Rehydrate(OrderDb record)
    {
        return Order.Rehydrate(
            UlidId.Parse(record.OrderId),
            UlidId.Parse(record.SubjectId),
            UlidId.Parse(record.CustomerId),
            record.PolicySnapshotId,
            Enum.Parse<OrderStatus>(record.Status),
            record.CreatedAt,
            record.LastUpdatedAt,
            record.PackageCode,
            record.LastStatusReason,
            string.IsNullOrWhiteSpace(record.BlockedFromStatus)
                ? null
                : Enum.Parse<OrderStatus>(record.BlockedFromStatus),
            string.IsNullOrWhiteSpace(record.ActiveIntakeSessionId)
                ? null
                : UlidId.Parse(record.ActiveIntakeSessionId),
            string.IsNullOrWhiteSpace(record.LastCompletedIntakeSessionId)
                ? null
                : UlidId.Parse(record.LastCompletedIntakeSessionId),
            record.InvitedAt,
            record.IntakeStartedAt,
            record.IntakeCompletedAt,
            record.ReadyForRoutingAt,
            record.ClosedAt,
            record.CanceledAt);
    }

    private static OrderDb ToEntity(Order order)
    {
        var entity = new OrderDb();
        ApplyState(order, entity);
        return entity;
    }

    private static void ApplyState(Order order, OrderDb record)
    {
        record.OrderId = order.Id.ToString();
        record.SubjectId = order.SubjectId.ToString();
        record.CustomerId = order.CustomerId.ToString();
        record.PolicySnapshotId = order.PolicySnapshotId;
        record.PackageCode = order.PackageCode;
        record.Status = order.Status.ToString();
        record.BlockedFromStatus = order.BlockedFromStatus?.ToString();
        record.LastStatusReason = order.LastStatusReason;
        record.CreatedAt = order.CreatedAt;
        record.LastUpdatedAt = order.LastUpdatedAt;
        record.ActiveIntakeSessionId = order.ActiveIntakeSessionId?.ToString();
        record.LastCompletedIntakeSessionId = order.LastCompletedIntakeSessionId?.ToString();
        record.InvitedAt = order.InvitedAt;
        record.IntakeStartedAt = order.IntakeStartedAt;
        record.IntakeCompletedAt = order.IntakeCompletedAt;
        record.ReadyForRoutingAt = order.ReadyForRoutingAt;
        record.ClosedAt = order.ClosedAt;
        record.CanceledAt = order.CanceledAt;
    }
}