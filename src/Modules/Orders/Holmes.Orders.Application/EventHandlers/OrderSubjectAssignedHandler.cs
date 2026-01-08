using Holmes.Orders.Contracts;
using Holmes.Orders.Domain;
using Holmes.Orders.Domain.Events;
using MediatR;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class OrderSubjectAssignedHandler(
    IOrdersUnitOfWork unitOfWork,
    IOrderSummaryWriter summaryWriter,
    IOrderTimelineWriter timelineWriter
) : INotificationHandler<OrderSubjectAssigned>
{
    public async Task Handle(OrderSubjectAssigned notification, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(notification.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        await summaryWriter.UpsertAsync(order, cancellationToken);
        await timelineWriter.WriteAsync(new OrderTimelineEntry(
            order.Id,
            "order.subject_assigned",
            "Subject assigned",
            "workflow",
            notification.AssignedAt,
            new { subjectId = notification.SubjectId.ToString() }), cancellationToken);
    }
}
