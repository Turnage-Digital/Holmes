using Holmes.Orders.Domain;
using Holmes.Subjects.Contracts.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Orders.Application.EventHandlers;

/// <summary>
///     Creates an order when a subject intake request is issued.
///     Keeps order creation within the Orders module boundary.
/// </summary>
public sealed class SubjectIntakeRequestedOrderHandler(
    IOrdersUnitOfWork unitOfWork,
    ILogger<SubjectIntakeRequestedOrderHandler> logger
) : INotificationHandler<SubjectIntakeRequestedIntegrationEvent>
{
    public async Task Handle(SubjectIntakeRequestedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        var existing = await unitOfWork.Orders.GetByIdAsync(
            notification.OrderId,
            cancellationToken);

        if (existing is not null)
        {
            logger.LogInformation(
                "Order {OrderId} already exists for subject intake request; skipping",
                notification.OrderId);
            return;
        }

        var order = Order.CreateFromIntake(
            notification.OrderId,
            notification.SubjectId,
            notification.CustomerId,
            notification.PolicySnapshotId,
            notification.RequestedAt,
            notification.RequestedBy);

        await unitOfWork.Orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);

        logger.LogInformation(
            "Created Order {OrderId} for Subject {SubjectId}",
            notification.OrderId,
            notification.SubjectId);
    }
}