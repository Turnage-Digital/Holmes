using Holmes.Core.Domain;

namespace Holmes.Notifications.Domain;

public interface INotificationsUnitOfWork : IUnitOfWork
{
    INotificationRepository Notifications { get; }
}