using Holmes.SlaClocks.Application.Abstractions;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Domain.Events;
using MediatR;

namespace Holmes.SlaClocks.Application.EventHandlers;

/// <summary>
///     Handles SLA clock domain events to maintain the sla_clock_projections table.
/// </summary>
public sealed class SlaClockProjectionHandler(
    ISlaClockProjectionWriter writer,
    ISlaClockQueries queries
)
    : INotificationHandler<SlaClockStarted>,
        INotificationHandler<SlaClockPaused>,
        INotificationHandler<SlaClockResumed>,
        INotificationHandler<SlaClockAtRisk>,
        INotificationHandler<SlaClockBreached>,
        INotificationHandler<SlaClockCompleted>
{
    public Task Handle(SlaClockAtRisk notification, CancellationToken cancellationToken)
    {
        return writer.UpdateAtRiskAsync(
            notification.ClockId.ToString(),
            ClockState.AtRisk,
            notification.AtRiskAt,
            cancellationToken);
    }

    public Task Handle(SlaClockBreached notification, CancellationToken cancellationToken)
    {
        return writer.UpdateBreachedAsync(
            notification.ClockId.ToString(),
            ClockState.Breached,
            notification.BreachedAt,
            cancellationToken);
    }

    public Task Handle(SlaClockCompleted notification, CancellationToken cancellationToken)
    {
        return writer.UpdateCompletedAsync(
            notification.ClockId.ToString(),
            ClockState.Completed,
            notification.CompletedAt,
            cancellationToken);
    }

    public Task Handle(SlaClockPaused notification, CancellationToken cancellationToken)
    {
        return writer.UpdatePauseInfoAsync(
            notification.ClockId.ToString(),
            ClockState.Paused,
            notification.PausedAt,
            notification.Reason,
            cancellationToken);
    }

    public async Task Handle(SlaClockResumed notification, CancellationToken cancellationToken)
    {
        // Fetch current state from the write model to get accurate timing info
        // The Resume event only carries PauseDuration, but we need the updated deadline/threshold
        var clock = await queries.GetByOrderIdAndKindAsync(
            notification.OrderId.ToString(),
            notification.Kind,
            cancellationToken);

        if (clock is null)
        {
            // Fall back to simple state update if write model not found
            await writer.UpdateStateAsync(
                notification.ClockId.ToString(),
                ClockState.Running,
                cancellationToken);
            return;
        }

        // Use state from the write model which has already been updated
        await writer.UpdateResumeInfoAsync(
            notification.ClockId.ToString(),
            clock.State,
            clock.DeadlineAt,
            clock.AtRiskThresholdAt,
            clock.AccumulatedPauseTime,
            cancellationToken);
    }

    public Task Handle(SlaClockStarted notification, CancellationToken cancellationToken)
    {
        var model = new SlaClockProjectionModel(
            notification.ClockId.ToString(),
            notification.OrderId.ToString(),
            notification.CustomerId.ToString(),
            notification.Kind,
            ClockState.Running,
            notification.StartedAt,
            notification.DeadlineAt,
            notification.AtRiskThresholdAt,
            notification.TargetBusinessDays,
            0.80m // Default at-risk threshold (event doesn't carry this)
        );

        return writer.UpsertAsync(model, cancellationToken);
    }
}