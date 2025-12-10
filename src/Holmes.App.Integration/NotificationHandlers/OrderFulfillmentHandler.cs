using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Application.Commands;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Integration.NotificationHandlers;

/// <summary>
///     Handles OrderStatusChanged events to dispatch service requests when an order
///     reaches ReadyForFulfillment. Creates ServiceRequest for each enabled service
///     in the customer's catalog.
/// </summary>
public sealed class OrderFulfillmentHandler(
    IServiceCatalogQueries catalogQueries,
    ISender sender,
    ILogger<OrderFulfillmentHandler> logger
) : INotificationHandler<OrderStatusChanged>
{
    public async Task Handle(OrderStatusChanged notification, CancellationToken cancellationToken)
    {
        if (notification.Status != OrderStatus.ReadyForFulfillment)
        {
            return;
        }

        logger.LogInformation(
            "Order {OrderId} ready for fulfillment, dispatching services for Customer {CustomerId}",
            notification.OrderId,
            notification.CustomerId);

        var catalog = await catalogQueries.GetByCustomerIdAsync(
            notification.CustomerId.ToString(),
            cancellationToken);

        var enabledServices = catalog.Services
            .Where(s => s.IsEnabled)
            .ToList();

        if (enabledServices.Count == 0)
        {
            logger.LogWarning(
                "Customer {CustomerId} has no enabled services in catalog, order {OrderId} has no services to fulfill",
                notification.CustomerId,
                notification.OrderId);
            return;
        }

        logger.LogInformation(
            "Creating {Count} service requests for Order {OrderId}",
            enabledServices.Count,
            notification.OrderId);

        foreach (var service in enabledServices)
        {
            var command = new CreateServiceRequestCommand(
                notification.OrderId,
                notification.CustomerId,
                service.ServiceTypeCode,
                service.Tier,
                null, // No scope for now
                null, // No catalog snapshot ID for now
                notification.ChangedAt);

            var result = await sender.Send(command, cancellationToken);

            if (result.IsSuccess)
            {
                logger.LogDebug(
                    "Created ServiceRequest {ServiceRequestId} for {ServiceType} on Order {OrderId}",
                    result.Value,
                    service.ServiceTypeCode,
                    notification.OrderId);
            }
            else
            {
                logger.LogWarning(
                    "Failed to create ServiceRequest for {ServiceType} on Order {OrderId}: {Error}",
                    service.ServiceTypeCode,
                    notification.OrderId,
                    result.Error);
            }
        }
    }
}
