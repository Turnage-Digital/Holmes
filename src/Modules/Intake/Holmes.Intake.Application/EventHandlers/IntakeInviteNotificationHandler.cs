using Holmes.Intake.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Intake.Application.EventHandlers;

/// <summary>
///     Stub handler for sending intake invite notifications to candidates.
///     TODO: Move to Notification Module when implemented.
/// </summary>
public sealed class IntakeInviteNotificationHandler(
    ILogger<IntakeInviteNotificationHandler> logger
) : INotificationHandler<IntakeSessionInvited>
{
    public Task Handle(IntakeSessionInvited notification, CancellationToken cancellationToken)
    {
        // TODO: Implement notification delivery via Notification Module
        // 1. Look up subject contact info (email/phone) from Subjects module
        // 2. Build intake URL: {baseUrl}/intake/{sessionId}?token={resumeToken}
        // 3. Send via configured channel (email via SendGrid, SMS, etc.)

        logger.LogDebug(
            "Intake invite notification pending for Subject {SubjectId}, Session {SessionId}, expires {ExpiresAt}",
            notification.SubjectId,
            notification.IntakeSessionId,
            notification.ExpiresAt);

        return Task.CompletedTask;
    }
}