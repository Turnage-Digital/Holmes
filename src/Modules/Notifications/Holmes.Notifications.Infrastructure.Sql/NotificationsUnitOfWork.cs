using Holmes.Core.Contracts;
using Holmes.Core.Contracts.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Notifications.Domain;
using MediatR;

namespace Holmes.Notifications.Infrastructure.Sql;

public sealed class NotificationsUnitOfWork(
    NotificationsDbContext context,
    IMediator mediator,
    INotificationRepository notificationRepository,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<NotificationsDbContext>(context, mediator, eventStore, serializer, tenantContext),
        INotificationsUnitOfWork
{
    public INotificationRepository Notifications => notificationRepository;
}