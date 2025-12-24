using Holmes.Core.Domain.Specifications;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql.Entities;

namespace Holmes.Notifications.Infrastructure.Sql.Specifications;

/// <summary>
///     Finds failed notifications that are eligible for retry (under max attempts and past retry delay).
/// </summary>
public sealed class FailedNotificationsForRetrySpec : Specification<NotificationDb>
{
    public FailedNotificationsForRetrySpec(int maxAttempts, DateTime lastAttemptBefore, int limit)
    {
        AddCriteria(n =>
            n.Status == (int)DeliveryStatus.Failed &&
            n.DeliveryAttempts.Count < maxAttempts &&
            n.DeliveryAttempts.Max(a => a.AttemptedAt) < lastAttemptBefore);

        ApplyOrderBy(n => n.CreatedAt);
        ApplyPaging(0, limit);
        AddInclude(n => n.DeliveryAttempts);
    }
}