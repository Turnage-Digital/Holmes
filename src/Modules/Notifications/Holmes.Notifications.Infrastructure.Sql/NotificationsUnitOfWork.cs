using Holmes.Core.Infrastructure.Sql;
using Holmes.Notifications.Domain;
using MediatR;

namespace Holmes.Notifications.Infrastructure.Sql;

public sealed class NotificationsUnitOfWork(
    NotificationsDbContext context,
    IMediator mediator,
    INotificationRequestRepository notificationRequestRepository
)
    : UnitOfWork<NotificationsDbContext>(context, mediator), INotificationsUnitOfWork
{
    public INotificationRequestRepository NotificationRequests => notificationRequestRepository;
}