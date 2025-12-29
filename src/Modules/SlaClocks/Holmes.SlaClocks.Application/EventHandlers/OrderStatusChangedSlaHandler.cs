using Holmes.Orders.Application.Abstractions.IntegrationEvents;
using Holmes.SlaClocks.Application.Abstractions;
using Holmes.SlaClocks.Application.Commands;
using Holmes.SlaClocks.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.SlaClocks.Application.EventHandlers;

/// <summary>
///     Handles OrderStatusChanged events to start, complete, pause, and resume SLA clocks.
/// </summary>
public sealed class OrderStatusChangedSlaHandler(
    ISlaClockQueries slaClockQueries,
    ISender sender,
    ILogger<OrderStatusChangedSlaHandler> logger
) : INotificationHandler<OrderStatusChangedIntegrationEvent>
{
    private const string CreatedStatus = "Created";
    private const string InvitedStatus = "Invited";
    private const string IntakeCompleteStatus = "IntakeComplete";
    private const string ReadyForFulfillmentStatus = "ReadyForFulfillment";
    private const string ReadyForReportStatus = "ReadyForReport";
    private const string ClosedStatus = "Closed";
    private const string BlockedStatus = "Blocked";
    private const string CanceledStatus = "Canceled";

    public async Task Handle(OrderStatusChangedIntegrationEvent notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Processing OrderStatusChanged for SLA clocks: OrderId={OrderId}, Status={Status}",
            notification.OrderId, notification.Status);

        switch (notification.Status)
        {
            case CreatedStatus:
                await sender.Send(new StartSlaClockCommand(
                    notification.OrderId,
                    notification.CustomerId,
                    ClockKind.Overall,
                    notification.ChangedAt), cancellationToken);
                break;

            case InvitedStatus:
                await sender.Send(new StartSlaClockCommand(
                    notification.OrderId,
                    notification.CustomerId,
                    ClockKind.Intake,
                    notification.ChangedAt), cancellationToken);
                break;

            case IntakeCompleteStatus:
                await sender.Send(new CompleteSlaClockCommand(
                    notification.OrderId,
                    ClockKind.Intake,
                    notification.ChangedAt), cancellationToken);
                break;

            case ReadyForFulfillmentStatus:
                await sender.Send(new StartSlaClockCommand(
                    notification.OrderId,
                    notification.CustomerId,
                    ClockKind.Fulfillment,
                    notification.ChangedAt), cancellationToken);
                break;

            case ReadyForReportStatus:
                await sender.Send(new CompleteSlaClockCommand(
                    notification.OrderId,
                    ClockKind.Fulfillment,
                    notification.ChangedAt), cancellationToken);
                break;

            case ClosedStatus:
                await sender.Send(new CompleteSlaClockCommand(
                    notification.OrderId,
                    ClockKind.Overall,
                    notification.ChangedAt), cancellationToken);
                break;

            case BlockedStatus:
                await PauseAllActiveClocksAsync(notification, cancellationToken);
                break;

            case CanceledStatus:
                await CompleteAllActiveClocksAsync(notification, cancellationToken);
                break;
        }

        // Handle resume from block - check if any paused clocks need resuming
        // When order leaves Blocked state, we need to resume paused clocks
        if (!string.Equals(notification.Status, BlockedStatus, StringComparison.Ordinal) &&
            !string.Equals(notification.Status, CanceledStatus, StringComparison.Ordinal) &&
            !string.Equals(notification.Status, ClosedStatus, StringComparison.Ordinal))
        {
            await ResumeAnyPausedClocksAsync(notification, cancellationToken);
        }
    }

    private async Task PauseAllActiveClocksAsync(
        OrderStatusChangedIntegrationEvent notification,
        CancellationToken cancellationToken
    )
    {
        var activeClocks = await slaClockQueries.GetActiveByOrderIdAsync(
            notification.OrderId.ToString(), cancellationToken);

        foreach (var clock in activeClocks)
        {
            await sender.Send(new PauseSlaClockCommand(
                clock.Id,
                notification.Reason,
                notification.ChangedAt), cancellationToken);
        }
    }

    private async Task CompleteAllActiveClocksAsync(
        OrderStatusChangedIntegrationEvent notification,
        CancellationToken cancellationToken
    )
    {
        var activeClocks = await slaClockQueries.GetActiveByOrderIdAsync(
            notification.OrderId.ToString(), cancellationToken);

        foreach (var clock in activeClocks)
        {
            await sender.Send(new CompleteSlaClockCommand(
                notification.OrderId,
                clock.Kind,
                notification.ChangedAt), cancellationToken);
        }
    }

    private async Task ResumeAnyPausedClocksAsync(
        OrderStatusChangedIntegrationEvent notification,
        CancellationToken cancellationToken
    )
    {
        var clocks = await slaClockQueries.GetByOrderIdAsync(
            notification.OrderId.ToString(), cancellationToken);
        var pausedClocks = clocks.Where(c => c.State == ClockState.Paused);

        foreach (var clock in pausedClocks)
        {
            await sender.Send(new ResumeSlaClockCommand(
                clock.Id,
                notification.ChangedAt), cancellationToken);
        }
    }
}
