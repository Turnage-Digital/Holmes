using Holmes.Core.Domain.ValueObjects;
using Holmes.Workflow.Application.Abstractions.Notifications;
using Holmes.Workflow.Application.Abstractions.Projections;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Domain.Events;
using MediatR;

namespace Holmes.Workflow.Application.EventHandlers;

public sealed class OrderStatusChangedHandler(
    IWorkflowUnitOfWork unitOfWork,
    IOrderSummaryWriter summaryWriter,
    IOrderChangeBroadcaster broadcaster,
    IOrderTimelineWriter timelineWriter
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
        await timelineWriter.WriteAsync(new OrderTimelineEntry(
            order.Id,
            "order.status_changed",
            notification.Reason,
            "workflow",
            notification.ChangedAt,
            new { status = order.Status.ToString() }), cancellationToken);
        await broadcaster.PublishAsync(new OrderChange(
            UlidId.NewUlid(),
            order.Id,
            order.Status,
            notification.Reason,
            notification.ChangedAt), cancellationToken);
    }
}