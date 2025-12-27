using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Application.Abstractions.Commands;
using Holmes.Orders.Application.Abstractions.Commands;
using Holmes.Orders.Domain;
using Holmes.Orders.Domain.Events;
using Holmes.Services.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Application.EventHandlers;

/// <summary>
///     Handles OrderStatusChanged events to dispatch services when an order
///     reaches ReadyForFulfillment. Creates Service for each enabled service
///     in the customer's catalog, then transitions the order to FulfillmentInProgress.
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
            "Creating {Count} services for Order {OrderId}",
            enabledServices.Count,
            notification.OrderId);

        var successCount = 0;
        foreach (var service in enabledServices)
        {
            var command = new CreateServiceCommand(
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
                successCount++;
                logger.LogDebug(
                    "Created Service {ServiceId} for {ServiceType} on Order {OrderId}",
                    result.Value,
                    service.ServiceTypeCode,
                    notification.OrderId);
            }
            else
            {
                logger.LogWarning(
                    "Failed to create Service for {ServiceType} on Order {OrderId}: {Error}",
                    service.ServiceTypeCode,
                    notification.OrderId,
                    result.Error);
            }
        }

        // Only transition to FulfillmentInProgress if at least one service was created
        if (successCount > 0)
        {
            var beginFulfillmentCommand = new BeginOrderFulfillmentCommand(
                notification.OrderId,
                notification.ChangedAt,
                $"Fulfillment started with {successCount} service(s)");

            var beginResult = await sender.Send(beginFulfillmentCommand, cancellationToken);

            if (beginResult.IsSuccess)
            {
                logger.LogInformation(
                    "Order {OrderId} transitioned to FulfillmentInProgress with {Count} services",
                    notification.OrderId,
                    successCount);
            }
            else
            {
                logger.LogError(
                    "Failed to transition Order {OrderId} to FulfillmentInProgress: {Error}",
                    notification.OrderId,
                    beginResult.Error);
            }
        }
        else
        {
            logger.LogWarning(
                "No services were successfully created for Order {OrderId}, order will not advance",
                notification.OrderId);
        }
    }
}