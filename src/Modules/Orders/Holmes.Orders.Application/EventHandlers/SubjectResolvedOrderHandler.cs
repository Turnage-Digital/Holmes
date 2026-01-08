using Holmes.Orders.Domain;
using Holmes.Subjects.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class SubjectResolvedOrderHandler(
    IOrdersUnitOfWork unitOfWork,
    ILogger<SubjectResolvedOrderHandler> logger
) : INotificationHandler<SubjectResolvedIntegrationEvent>
{
    public async Task Handle(SubjectResolvedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var order = await unitOfWork.Orders.GetByIdAsync(notification.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        var assigned = order.AssignSubject(notification.SubjectId, notification.OccurredAt);
        if (!assigned)
        {
            logger.LogWarning(
                "Order {OrderId} already assigned to a different subject, skipping update",
                notification.OrderId);
            return;
        }

        await unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
