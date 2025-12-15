using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Application.Abstractions.Notifications;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Domain.Events;
using MediatR;

namespace Holmes.SlaClocks.Application.EventHandlers;

/// <summary>
///     Handles SLA clock domain events to broadcast real-time updates via SSE.
/// </summary>
public sealed class SlaClockChangeBroadcastHandler(
    ISlaClockChangeBroadcaster broadcaster
)
    : INotificationHandler<SlaClockStarted>,
        INotificationHandler<SlaClockPaused>,
        INotificationHandler<SlaClockResumed>,
        INotificationHandler<SlaClockAtRisk>,
        INotificationHandler<SlaClockBreached>,
        INotificationHandler<SlaClockCompleted>
{
    public Task Handle(SlaClockStarted notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new SlaClockChange(
            UlidId.NewUlid(),
            notification.ClockId,
            notification.OrderId,
            notification.CustomerId,
            notification.Kind,
            ClockState.Running,
            $"Started with {notification.TargetBusinessDays} business day target",
            notification.StartedAt), cancellationToken);
    }

    public Task Handle(SlaClockPaused notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new SlaClockChange(
            UlidId.NewUlid(),
            notification.ClockId,
            notification.OrderId,
            notification.CustomerId,
            notification.Kind,
            ClockState.Paused,
            notification.Reason,
            notification.PausedAt), cancellationToken);
    }

    public Task Handle(SlaClockResumed notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new SlaClockChange(
            UlidId.NewUlid(),
            notification.ClockId,
            notification.OrderId,
            notification.CustomerId,
            notification.Kind,
            ClockState.Running,
            $"Resumed after {notification.PauseDuration.TotalMinutes:F0} minutes paused",
            notification.ResumedAt), cancellationToken);
    }

    public Task Handle(SlaClockAtRisk notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new SlaClockChange(
            UlidId.NewUlid(),
            notification.ClockId,
            notification.OrderId,
            notification.CustomerId,
            notification.Kind,
            ClockState.AtRisk,
            $"Approaching deadline at {notification.DeadlineAt:g}",
            notification.AtRiskAt), cancellationToken);
    }

    public Task Handle(SlaClockBreached notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new SlaClockChange(
            UlidId.NewUlid(),
            notification.ClockId,
            notification.OrderId,
            notification.CustomerId,
            notification.Kind,
            ClockState.Breached,
            $"Deadline of {notification.DeadlineAt:g} was breached",
            notification.BreachedAt), cancellationToken);
    }

    public Task Handle(SlaClockCompleted notification, CancellationToken cancellationToken)
    {
        var reason = notification.WasAtRisk
            ? $"Completed in {notification.TotalElapsed.TotalHours:F1}h (was at risk)"
            : $"Completed in {notification.TotalElapsed.TotalHours:F1}h";

        return broadcaster.PublishAsync(new SlaClockChange(
            UlidId.NewUlid(),
            notification.ClockId,
            notification.OrderId,
            notification.CustomerId,
            notification.Kind,
            ClockState.Completed,
            reason,
            notification.CompletedAt), cancellationToken);
    }
}
