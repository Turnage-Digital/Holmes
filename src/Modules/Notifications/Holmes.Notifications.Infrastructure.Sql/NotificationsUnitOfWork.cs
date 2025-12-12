using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Notifications.Domain;
using MediatR;

namespace Holmes.Notifications.Infrastructure.Sql;

public sealed class NotificationsUnitOfWork(
    NotificationsDbContext context,
    IMediator mediator,
    INotificationRequestRepository notificationRequestRepository,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null
)
    : UnitOfWork<NotificationsDbContext>(context, mediator, eventStore, serializer, tenantContext),
        INotificationsUnitOfWork
{
    public INotificationRequestRepository NotificationRequests => notificationRequestRepository;
}