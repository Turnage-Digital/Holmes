using Holmes.Workflow.Application.Notifications;
using Holmes.Workflow.Application.Projections;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Domain.Events;
using MediatR;

namespace Holmes.Workflow.Application.EventHandlers;

public sealed class OrderStatusChangedHandler(
    IWorkflowUnitOfWork unitOfWork,
    IOrderSummaryWriter summaryWriter,
    IOrderChangeBroadcaster broadcaster
) : INotificationHandler<OrderStatusChanged>
{
    public async Task Handle(OrderStatusChanged notification, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(notification.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        await summaryWriter.UpsertAsync(order, cancellationToken);
        await broadcaster.PublishAsync(new OrderChange(
            order.Id,
            order.Status,
            notification.Reason,
            notification.ChangedAt), cancellationToken);
    }
}