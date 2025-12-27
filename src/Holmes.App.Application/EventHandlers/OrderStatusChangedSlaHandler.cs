using Holmes.Orders.Domain;
using Holmes.Orders.Domain.Events;
using Holmes.SlaClocks.Application.Abstractions;
using Holmes.SlaClocks.Application.Abstractions.Commands;
using Holmes.SlaClocks.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Application.EventHandlers;

/// <summary>
///     Handles OrderStatusChanged events to start, complete, pause, and resume SLA clocks.
///     Lives in App.Integration because it crosses the Workflow → SlaClocks boundary.
/// </summary>
public sealed class OrderStatusChangedSlaHandler(
    ISlaClockQueries slaClockQueries,
    ISender sender,
    ILogger<OrderStatusChangedSlaHandler> logger
) : INotificationHandler<OrderStatusChanged>
{
    public async Task Handle(OrderStatusChanged notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Processing OrderStatusChanged for SLA clocks: OrderId={OrderId}, Status={Status}",
            notification.OrderId, notification.Status);

        switch (notification.Status)
        {
            case OrderStatus.Created:
                await sender.Send(new StartSlaClockCommand(
                    notification.OrderId,
                    notification.CustomerId,
                    ClockKind.Overall,
                    notification.ChangedAt), cancellationToken);
                break;

            case OrderStatus.Invited:
                await sender.Send(new StartSlaClockCommand(
                    notification.OrderId,
                    notification.CustomerId,
                    ClockKind.Intake,
                    notification.ChangedAt), cancellationToken);
                break;

            case OrderStatus.IntakeComplete:
                await sender.Send(new CompleteSlaClockCommand(
                    notification.OrderId,
                    ClockKind.Intake,
                    notification.ChangedAt), cancellationToken);
                break;

            case OrderStatus.ReadyForFulfillment:
                await sender.Send(new StartSlaClockCommand(
                    notification.OrderId,
                    notification.CustomerId,
                    ClockKind.Fulfillment,
                    notification.ChangedAt), cancellationToken);
                break;

            case OrderStatus.ReadyForReport:
                await sender.Send(new CompleteSlaClockCommand(
                    notification.OrderId,
                    ClockKind.Fulfillment,
                    notification.ChangedAt), cancellationToken);
                break;

            case OrderStatus.Closed:
                await sender.Send(new CompleteSlaClockCommand(
                    notification.OrderId,
                    ClockKind.Overall,
                    notification.ChangedAt), cancellationToken);
                break;

            case OrderStatus.Blocked:
                await PauseAllActiveClocksAsync(notification, cancellationToken);
                break;

            case OrderStatus.Canceled:
                await CompleteAllActiveClocksAsync(notification, cancellationToken);
                break;
        }

        // Handle resume from block - check if any paused clocks need resuming
        // When order leaves Blocked state, we need to resume paused clocks
        if (notification.Status is not OrderStatus.Blocked and not OrderStatus.Canceled and not OrderStatus.Closed)
        {
            await ResumeAnyPausedClocksAsync(notification, cancellationToken);
        }
    }

    private async Task PauseAllActiveClocksAsync(OrderStatusChanged notification, CancellationToken cancellationToken)
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
        OrderStatusChanged notification,
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

    private async Task ResumeAnyPausedClocksAsync(OrderStatusChanged notification, CancellationToken cancellationToken)
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