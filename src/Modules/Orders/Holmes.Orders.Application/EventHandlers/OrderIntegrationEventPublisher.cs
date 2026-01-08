using Holmes.Orders.Contracts.IntegrationEvents;
using Holmes.Orders.Domain.Events;
using MediatR;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class OrderIntegrationEventPublisher(
    IMediator mediator
) : INotificationHandler<OrderRequested>,
    INotificationHandler<OrderSubjectAssigned>,
    INotificationHandler<OrderStatusChanged>
{
    public Task Handle(OrderRequested notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new OrderRequestedIntegrationEvent(
            notification.OrderId,
            notification.CustomerId,
            notification.SubjectEmail,
            notification.SubjectPhone,
            notification.PolicySnapshotId,
            notification.PackageCode,
            notification.RequestedAt,
            notification.RequestedBy), cancellationToken);
    }

    public Task Handle(OrderSubjectAssigned notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new OrderSubjectAssignedIntegrationEvent(
            notification.OrderId,
            notification.CustomerId,
            notification.SubjectId,
            notification.AssignedAt), cancellationToken);
    }

    public Task Handle(OrderStatusChanged notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new OrderStatusChangedIntegrationEvent(
            notification.OrderId,
            notification.CustomerId,
            notification.Status.ToString(),
            notification.Reason,
            notification.ChangedAt), cancellationToken);
    }
}
