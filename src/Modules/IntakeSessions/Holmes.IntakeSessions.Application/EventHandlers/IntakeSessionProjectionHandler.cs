using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Abstractions;
using Holmes.IntakeSessions.Application.Projections;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.IntakeSessions.Application.EventHandlers;

public sealed class IntakeSessionProjectionHandler(
    IIntakeSessionProjectionWriter writer,
    ILogger<IntakeSessionProjectionHandler> logger
) : INotificationHandler<IntakeSessionInvited>,
    INotificationHandler<IntakeSessionStarted>,
    INotificationHandler<IntakeProgressSaved>,
    INotificationHandler<ConsentCaptured>,
    INotificationHandler<IntakeSubmissionReceived>,
    INotificationHandler<IntakeSubmissionAccepted>,
    INotificationHandler<IntakeSessionExpired>,
    INotificationHandler<IntakeSessionSuperseded>
{
    public async Task Handle(ConsentCaptured notification, CancellationToken cancellationToken)
    {
        await UpdateAsync(notification.IntakeSessionId, projection =>
            projection with { LastTouchedAt = notification.Artifact.CapturedAt }, cancellationToken);
        RecordMetric("consent_captured");
    }

    public async Task Handle(IntakeProgressSaved notification, CancellationToken cancellationToken)
    {
        await UpdateAsync(notification.IntakeSessionId, projection =>
            projection with { LastTouchedAt = notification.AnswersSnapshot.UpdatedAt }, cancellationToken);
        RecordMetric("progress_saved");
    }

    public async Task Handle(IntakeSessionExpired notification, CancellationToken cancellationToken)
    {
        await UpdateAsync(notification.IntakeSessionId, projection =>
            projection with
            {
                Status = IntakeSessionStatus.Abandoned,
                LastTouchedAt = notification.ExpiredAt,
                CancellationReason = notification.Reason
            }, cancellationToken);
        RecordMetric("expired");
    }

    public async Task Handle(IntakeSessionInvited notification, CancellationToken cancellationToken)
    {
        var model = new IntakeSessionProjectionModel(
            notification.IntakeSessionId,
            notification.OrderId,
            notification.SubjectId,
            notification.CustomerId,
            notification.PolicySnapshot.SnapshotId,
            notification.PolicySnapshot.SchemaVersion,
            IntakeSessionStatus.Invited,
            notification.InvitedAt,
            notification.InvitedAt,
            notification.ExpiresAt,
            null,
            null,
            null,
            null);

        await writer.CreateAsync(model, cancellationToken);
        RecordMetric("invited");
    }

    public async Task Handle(IntakeSessionStarted notification, CancellationToken cancellationToken)
    {
        await UpdateAsync(notification.IntakeSessionId, projection =>
                projection with
                {
                    Status = IntakeSessionStatus.InProgress,
                    LastTouchedAt = notification.StartedAt
                },
            cancellationToken);
        RecordMetric("started");
    }

    public async Task Handle(IntakeSessionSuperseded notification, CancellationToken cancellationToken)
    {
        await UpdateAsync(notification.IntakeSessionId, projection =>
            projection with
            {
                Status = IntakeSessionStatus.Abandoned,
                LastTouchedAt = notification.SupersededAt,
                SupersededBySessionId = notification.SupersededByIntakeSessionId
            }, cancellationToken);
        RecordMetric("superseded");
    }

    public async Task Handle(IntakeSubmissionAccepted notification, CancellationToken cancellationToken)
    {
        await UpdateAsync(notification.IntakeSessionId, projection =>
            projection with
            {
                Status = IntakeSessionStatus.Submitted,
                LastTouchedAt = notification.AcceptedAt,
                AcceptedAt = notification.AcceptedAt
            }, cancellationToken);
        RecordMetric("submission_accepted");
    }

    public async Task Handle(IntakeSubmissionReceived notification, CancellationToken cancellationToken)
    {
        await UpdateAsync(notification.IntakeSessionId, projection =>
            projection with
            {
                Status = IntakeSessionStatus.AwaitingReview,
                LastTouchedAt = notification.SubmittedAt,
                SubmittedAt = notification.SubmittedAt
            }, cancellationToken);
        RecordMetric("submission_received");
    }

    private async Task UpdateAsync(
        UlidId intakeSessionId,
        Func<IntakeSessionProjectionModel, IntakeSessionProjectionModel> updater,
        CancellationToken cancellationToken
    )
    {
        var updated = await writer.UpdateAsync(intakeSessionId, updater, cancellationToken);
        if (!updated)
        {
            logger.LogWarning("Projection update skipped for intake session {SessionId}", intakeSessionId);
        }
    }

    private static void RecordMetric(string eventName)
    {
        IntakeProjectionMetrics.ProjectionUpdates.Add(1,
            KeyValuePair.Create<string, object?>("event", eventName));
    }
}