using Holmes.IntakeSessions.Contracts.IntegrationEvents;
using Holmes.Orders.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class IntakeSessionOrderHandler(
    IOrdersUnitOfWork unitOfWork,
    ILogger<IntakeSessionOrderHandler> logger
) : INotificationHandler<IntakeSessionStartedIntegrationEvent>
{
    public async Task Handle(IntakeSessionStartedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "IntakeSessionStarted: Notifying Workflow of intake start for Order {OrderId}",
            notification.OrderId);

        var order = await unitOfWork.Orders.GetByIdAsync(notification.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        var linked = order.LinkIntakeSession(
            notification.IntakeSessionId,
            notification.OccurredAt,
            "Intake session started");
        if (!linked)
        {
            logger.LogWarning(
                "Order {OrderId} already linked to a different intake session, skipping update",
                notification.OrderId);
            return;
        }

        await unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
