using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain.Events;
using Holmes.Workflow.Application.Abstractions.Projections;
using MediatR;

namespace Holmes.Intake.Application.EventHandlers;

public sealed class IntakeTimelineHandler(IOrderTimelineWriter timelineWriter)
    : INotificationHandler<IntakeSessionInvited>,
        INotificationHandler<IntakeSessionStarted>,
        INotificationHandler<ConsentCaptured>,
        INotificationHandler<IntakeSubmissionReceived>,
        INotificationHandler<IntakeSubmissionAccepted>,
        INotificationHandler<IntakeSessionExpired>,
        INotificationHandler<IntakeSessionSuperseded>
{
    public Task Handle(ConsentCaptured notification, CancellationToken cancellationToken)
    {
        return WriteAsync(notification.OrderId,
            "intake.consent_captured",
            "Consent captured",
            notification.Artifact.CapturedAt,
            new
            {
                sessionId = notification.IntakeSessionId.ToString(),
                artifactId = notification.Artifact.ArtifactId.ToString()
            },
            cancellationToken);
    }

    public Task Handle(IntakeSessionExpired notification, CancellationToken cancellationToken)
    {
        return WriteAsync(notification.OrderId,
            "intake.session_expired",
            "Intake session expired",
            notification.ExpiredAt,
            new
            {
                sessionId = notification.IntakeSessionId.ToString(),
                reason = notification.Reason
            },
            cancellationToken);
    }

    public Task Handle(IntakeSessionInvited notification, CancellationToken cancellationToken)
    {
        return WriteAsync(notification.OrderId,
            "intake.session_invited",
            "Intake invite issued",
            notification.InvitedAt,
            new
            {
                sessionId = notification.IntakeSessionId.ToString(),
                expiresAt = notification.ExpiresAt
            },
            cancellationToken);
    }

    public Task Handle(IntakeSessionStarted notification, CancellationToken cancellationToken)
    {
        return WriteAsync(notification.OrderId,
            "intake.session_started",
            "Subject resumed intake",
            notification.StartedAt,
            new
            {
                sessionId = notification.IntakeSessionId.ToString(),
                device = notification.DeviceInfo
            },
            cancellationToken);
    }

    public Task Handle(IntakeSessionSuperseded notification, CancellationToken cancellationToken)
    {
        return WriteAsync(notification.OrderId,
            "intake.session_superseded",
            "Intake session superseded",
            notification.SupersededAt,
            new
            {
                sessionId = notification.IntakeSessionId.ToString(),
                supersededBy = notification.SupersededByIntakeSessionId.ToString()
            },
            cancellationToken);
    }

    public Task Handle(IntakeSubmissionAccepted notification, CancellationToken cancellationToken)
    {
        return WriteAsync(notification.OrderId,
            "intake.submission_accepted",
            "Intake accepted by operations",
            notification.AcceptedAt,
            new { sessionId = notification.IntakeSessionId.ToString() },
            cancellationToken);
    }

    public Task Handle(IntakeSubmissionReceived notification, CancellationToken cancellationToken)
    {
        return WriteAsync(notification.OrderId,
            "intake.submission_received",
            "Intake submitted",
            notification.SubmittedAt,
            new { sessionId = notification.IntakeSessionId.ToString() },
            cancellationToken);
    }

    private Task WriteAsync(
        UlidId orderId,
        string eventType,
        string description,
        DateTimeOffset occurredAt,
        object? metadata,
        CancellationToken cancellationToken
    )
    {
        var entry = new OrderTimelineEntry(
            orderId,
            eventType,
            description,
            "intake",
            occurredAt,
            metadata);
        return timelineWriter.WriteAsync(entry, cancellationToken);
    }
}