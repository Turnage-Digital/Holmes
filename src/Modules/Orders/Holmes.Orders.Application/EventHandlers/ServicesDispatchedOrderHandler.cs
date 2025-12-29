using Holmes.Orders.Application.Commands;
using Holmes.Services.Application.Abstractions.IntegrationEvents;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class ServicesDispatchedOrderHandler(
    ISender sender,
    ILogger<ServicesDispatchedOrderHandler> logger
) : INotificationHandler<ServicesDispatchedIntegrationEvent>
{
    public async Task Handle(
        ServicesDispatchedIntegrationEvent notification,
        CancellationToken cancellationToken
    )
    {
        var command = new BeginOrderFulfillmentCommand(
            notification.OrderId,
            notification.DispatchedAt,
            $"Fulfillment started with {notification.ServiceCount} service(s)");

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            var error = result.Error ?? "Failed to transition order to fulfillment.";
            logger.LogWarning(
                "Unable to begin fulfillment for Order {OrderId}: {Error}",
                notification.OrderId,
                error);
        }
    }
}
