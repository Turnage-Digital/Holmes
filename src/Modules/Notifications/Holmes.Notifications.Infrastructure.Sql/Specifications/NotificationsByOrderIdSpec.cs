using Holmes.Core.Domain.Specifications;
using Holmes.Notifications.Infrastructure.Sql.Entities;

namespace Holmes.Notifications.Infrastructure.Sql.Specifications;

/// <summary>
///     Finds all notifications for a specific order, ordered by creation date descending.
/// </summary>
public sealed class NotificationsByOrderIdSpec : Specification<NotificationRequestDb>
{
    public NotificationsByOrderIdSpec(string orderId)
    {
        AddCriteria(n => n.OrderId == orderId);
        ApplyOrderByDescending(n => n.CreatedAt);
        AddInclude(n => n.DeliveryAttempts);
    }
}