using Holmes.Orders.Application.Abstractions.IntegrationEvents;
using Holmes.Orders.Domain.Events;
using MediatR;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class OrderIntegrationEventPublisher(
    IMediator mediator
) : INotificationHandler<OrderCreatedFromIntake>,
    INotificationHandler<OrderStatusChanged>
{
    public Task Handle(OrderCreatedFromIntake notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new OrderCreatedFromIntakeIntegrationEvent(
            notification.OrderId,
            notification.SubjectId,
            notification.CustomerId,
            notification.PolicySnapshotId,
            notification.CreatedAt,
            notification.RequestedBy), cancellationToken);
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
