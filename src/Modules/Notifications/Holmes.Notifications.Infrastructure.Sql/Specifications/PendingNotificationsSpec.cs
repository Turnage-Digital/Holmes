using Holmes.Core.Domain.Specifications;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql.Entities;

namespace Holmes.Notifications.Infrastructure.Sql.Specifications;

/// <summary>
///     Finds pending notifications that are ready to be processed (scheduled time has passed or immediate).
/// </summary>
public sealed class PendingNotificationsSpec : Specification<NotificationDb>
{
    public PendingNotificationsSpec(DateTime asOfUtc, int limit)
    {
        AddCriteria(n =>
            n.Status == (int)DeliveryStatus.Pending &&
            (n.ScheduledFor == null || n.ScheduledFor <= asOfUtc));

        ApplyOrderBy(n => n.CreatedAt);
        ApplyPaging(0, limit);
    }
}