using Holmes.Core.Domain;
using Holmes.IntakeSessions.Contracts.IntegrationEvents;
using Holmes.Orders.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class IntakeSubmissionOrderHandler(
    ISender sender,
    ILogger<IntakeSubmissionOrderHandler> logger
) : INotificationHandler<IntakeSubmissionReceivedIntegrationEvent>,
    INotificationHandler<IntakeSubmissionAcceptedIntegrationEvent>
{
    public async Task Handle(
        IntakeSubmissionAcceptedIntegrationEvent notification,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Order {OrderId} intake session {SessionId} accepted",
            notification.OrderId,
            notification.IntakeSessionId);

        var command = new MarkOrderReadyForFulfillmentCommand(
            notification.OrderId,
            notification.AcceptedAt,
            "Intake accepted");
        command.UserId = SystemActors.System;

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            var error = result.Error ?? "Failed to advance order to ready_for_fulfillment.";
            logger.LogWarning("Unable to advance Order {OrderId}: {Error}", notification.OrderId, error);
            throw new InvalidOperationException(error);
        }
    }

    public async Task Handle(
        IntakeSubmissionReceivedIntegrationEvent notification,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Order {OrderId} intake session {SessionId} submitted",
            notification.OrderId,
            notification.IntakeSessionId);

        var command = new MarkOrderIntakeSubmittedCommand(
            notification.OrderId,
            notification.IntakeSessionId,
            notification.SubmittedAt,
            "Intake submission received");
        command.UserId = SystemActors.System;

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            var error = result.Error ?? "Failed to record intake submission on order.";
            logger.LogWarning("Unable to mark Order {OrderId} submitted: {Error}", notification.OrderId, error);
            throw new InvalidOperationException(error);
        }
    }
}