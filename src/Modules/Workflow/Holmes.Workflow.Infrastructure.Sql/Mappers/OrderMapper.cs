using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql.Entities;

namespace Holmes.Workflow.Infrastructure.Sql.Mappers;

public static class OrderMapper
{
    public static OrderDb ToDb(Order order)
    {
        var entity = new OrderDb();
        Apply(order, entity);
        return entity;
    }

    public static void Apply(Order order, OrderDb entity)
    {
        entity.OrderId = order.Id.ToString();
        entity.SubjectId = order.SubjectId.ToString();
        entity.CustomerId = order.CustomerId.ToString();
        entity.PolicySnapshotId = order.PolicySnapshotId;
        entity.PackageCode = order.PackageCode;
        entity.Status = order.Status.ToString();
        entity.BlockedFromStatus = order.BlockedFromStatus?.ToString();
        entity.LastStatusReason = order.LastStatusReason;
        entity.CreatedAt = order.CreatedAt;
        entity.LastUpdatedAt = order.LastUpdatedAt;
        entity.ActiveIntakeSessionId = order.ActiveIntakeSessionId?.ToString();
        entity.LastCompletedIntakeSessionId = order.LastCompletedIntakeSessionId?.ToString();
        entity.InvitedAt = order.InvitedAt;
        entity.IntakeStartedAt = order.IntakeStartedAt;
        entity.IntakeCompletedAt = order.IntakeCompletedAt;
        entity.ReadyForRoutingAt = order.ReadyForRoutingAt;
        entity.ClosedAt = order.ClosedAt;
        entity.CanceledAt = order.CanceledAt;
    }

    public static Order ToDomain(OrderDb record)
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
}