using Holmes.Core.Domain;
using Holmes.IntakeSessions.Contracts.IntegrationEvents;
using Holmes.Orders.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Orders.Application.EventHandlers;

public sealed class IntakeSessionOrderHandler(
    ISender sender,
    ILogger<IntakeSessionOrderHandler> logger
) : INotificationHandler<IntakeSessionInvitedIntegrationEvent>,
    INotificationHandler<IntakeSessionStartedIntegrationEvent>
{
    public async Task Handle(IntakeSessionInvitedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "IntakeSessionInvited: Notifying Workflow of invite for Order {OrderId}",
            notification.OrderId);

        var inviteCommand = new RecordOrderInviteCommand(
            notification.OrderId,
            notification.IntakeSessionId,
            notification.InvitedAt,
            "Intake invitation issued")
        {
            UserId = SystemActors.System
        };
        await sender.Send(inviteCommand, cancellationToken);
    }

    public async Task Handle(IntakeSessionStartedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "IntakeSessionStarted: Notifying Workflow of intake start for Order {OrderId}",
            notification.OrderId);

        var startCommand = new MarkOrderIntakeStartedCommand(
            notification.OrderId,
            notification.IntakeSessionId,
            notification.StartedAt,
            "Subject resumed intake")
        {
            UserId = SystemActors.System
        };
        await sender.Send(startCommand, cancellationToken);
    }
}