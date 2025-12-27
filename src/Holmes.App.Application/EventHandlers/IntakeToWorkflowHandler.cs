using Holmes.IntakeSessions.Domain.Events;
using Holmes.Orders.Application.Abstractions.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Application.EventHandlers;

/// <summary>
///     Bridges Intake domain events to Workflow commands.
///     Lives in App.Integration because it crosses the Intake → Workflow boundary.
/// </summary>
public sealed class IntakeToWorkflowHandler(
    ISender sender,
    ILogger<IntakeToWorkflowHandler> logger
) : INotificationHandler<IntakeSessionInvited>,
    INotificationHandler<IntakeSessionStarted>
{
    public async Task Handle(IntakeSessionInvited notification, CancellationToken cancellationToken)
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

    public async Task Handle(IntakeSessionStarted notification, CancellationToken cancellationToken)
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