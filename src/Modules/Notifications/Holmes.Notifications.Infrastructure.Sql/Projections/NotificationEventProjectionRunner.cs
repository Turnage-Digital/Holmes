using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Notifications.Infrastructure.Sql.Projections;

/// <summary>
///     Event-based projection runner for Notification projections.
///     Replays NotificationRequest domain events to rebuild the notification_projections table.
/// </summary>
public sealed class NotificationEventProjectionRunner : EventProjectionRunner
{
    private readonly NotificationsDbContext _notificationsDbContext;

    public NotificationEventProjectionRunner(
        NotificationsDbContext notificationsDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<NotificationEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        _notificationsDbContext = notificationsDbContext;
    }

    protected override string ProjectionName => "notifications.notification_projection.events";

    protected override string[]? StreamTypes => ["NotificationRequest"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_notificationsDbContext.Database.IsRelational())
        {
            await _notificationsDbContext.NotificationProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _notificationsDbContext.NotificationProjections.RemoveRange(_notificationsDbContext.NotificationProjections);
            await _notificationsDbContext.SaveChangesAsync(cancellationToken);
        }

        _notificationsDbContext.ChangeTracker.Clear();
    }
}
