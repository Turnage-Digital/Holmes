using Holmes.IntakeSessions.Contracts.IntegrationEvents;
using Holmes.Orders.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class IntakeSubmissionOrderHandler(
    IOrdersUnitOfWork unitOfWork,
    ILogger<IntakeSubmissionOrderHandler> logger
) : INotificationHandler<IntakeSubmittedIntegrationEvent>
{
    public async Task Handle(
        IntakeSubmittedIntegrationEvent notification,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Order {OrderId} intake session {SessionId} submitted",
            notification.OrderId,
            notification.IntakeSessionId);

        var order = await unitOfWork.Orders.GetByIdAsync(notification.OrderId, cancellationToken);
        if (order is null)
        {
            return;
        }

        if (order.Status == OrderStatus.Invited)
        {
            order.MarkIntakeInProgress(
                notification.IntakeSessionId,
                notification.OccurredAt,
                "Intake started");
        }

        order.MarkIntakeSubmitted(
            notification.IntakeSessionId,
            notification.OccurredAt,
            "Intake submission received");

        await unitOfWork.Orders.UpdateAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);
    }
}
