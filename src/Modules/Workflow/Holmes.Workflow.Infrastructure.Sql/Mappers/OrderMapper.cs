using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql.Entities;

namespace Holmes.Workflow.Infrastructure.Sql.Mappers;

public static class OrderMapper
{
    public static Order ToDomain(OrderDb db)
    {
        return Order.Rehydrate(
            UlidId.Parse(db.OrderId),
            UlidId.Parse(db.SubjectId),
            UlidId.Parse(db.CustomerId),
            db.PolicySnapshotId,
            Enum.Parse<OrderStatus>(db.Status),
            db.CreatedAt,
            db.LastUpdatedAt,
            db.PackageCode,
            db.LastStatusReason,
            string.IsNullOrWhiteSpace(db.BlockedFromStatus)
                ? null
                : Enum.Parse<OrderStatus>(db.BlockedFromStatus),
            string.IsNullOrWhiteSpace(db.ActiveIntakeSessionId)
                ? null
                : UlidId.Parse(db.ActiveIntakeSessionId),
            string.IsNullOrWhiteSpace(db.LastCompletedIntakeSessionId)
                ? null
                : UlidId.Parse(db.LastCompletedIntakeSessionId),
            db.InvitedAt,
            db.IntakeStartedAt,
            db.IntakeCompletedAt,
            db.ReadyForRoutingAt,
            db.ClosedAt,
            db.CanceledAt);
    }

    public static OrderDb ToDb(Order order)
    {
        var db = new OrderDb();
        UpdateDb(db, order);
        return db;
    }

    public static void UpdateDb(OrderDb db, Order order)
    {
        db.OrderId = order.Id.ToString();
        db.SubjectId = order.SubjectId.ToString();
        db.CustomerId = order.CustomerId.ToString();
        db.PolicySnapshotId = order.PolicySnapshotId;
        db.PackageCode = order.PackageCode;
        db.Status = order.Status.ToString();
        db.BlockedFromStatus = order.BlockedFromStatus?.ToString();
        db.LastStatusReason = order.LastStatusReason;
        db.CreatedAt = order.CreatedAt;
        db.LastUpdatedAt = order.LastUpdatedAt;
        db.ActiveIntakeSessionId = order.ActiveIntakeSessionId?.ToString();
        db.LastCompletedIntakeSessionId = order.LastCompletedIntakeSessionId?.ToString();
        db.InvitedAt = order.InvitedAt;
        db.IntakeStartedAt = order.IntakeStartedAt;
        db.IntakeCompletedAt = order.IntakeCompletedAt;
        db.ReadyForRoutingAt = order.ReadyForRoutingAt;
        db.ClosedAt = order.ClosedAt;
        db.CanceledAt = order.CanceledAt;
    }
}
