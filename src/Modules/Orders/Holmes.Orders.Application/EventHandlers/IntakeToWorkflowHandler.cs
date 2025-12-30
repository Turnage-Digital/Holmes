using Holmes.IntakeSessions.Contracts.IntegrationEvents;
using Holmes.Orders.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Orders.Application.EventHandlers;

/// <summary>
///     Bridges Intake integration events to Workflow commands.
/// </summary>
public sealed class IntakeToWorkflowHandler(
    ISender sender,
    ILogger<IntakeToWorkflowHandler> logger
) : INotificationHandler<IntakeSessionInvitedIntegrationEvent>,
    INotificationHandler<IntakeSessionStartedIntegrationEvent>
{
    public async Task Handle(IntakeSessionInvitedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "IntakeSessionInvited: Notifying Workflow of invite for Order {OrderId}",
            notification.OrderId);

        await sender.Send(new RecordOrderInviteCommand(
            notification.OrderId,
            notification.IntakeSessionId,
            notification.InvitedAt,
            "Intake invitation issued"), cancellationToken);
    }

    public async Task Handle(IntakeSessionStartedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "IntakeSessionStarted: Notifying Workflow of intake start for Order {OrderId}",
            notification.OrderId);

        await sender.Send(new MarkOrderIntakeStartedCommand(
            notification.OrderId,
            notification.IntakeSessionId,
            notification.StartedAt,
            "Subject resumed intake"), cancellationToken);
    }
}