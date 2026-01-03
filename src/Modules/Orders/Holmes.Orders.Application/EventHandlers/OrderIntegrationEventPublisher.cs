using Holmes.Orders.Contracts.IntegrationEvents;
using Holmes.Orders.Domain.Events;
using MediatR;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class OrderIntegrationEventPublisher(
    IMediator mediator
) : INotificationHandler<OrderCreated>,
    INotificationHandler<OrderStatusChanged>
{
    public Task Handle(OrderCreated notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new OrderCreatedIntegrationEvent(
            notification.OrderId,
            notification.SubjectId,
            notification.CustomerId,
            notification.PolicySnapshotId,
            notification.CreatedAt,
            notification.CreatedBy), cancellationToken);
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
