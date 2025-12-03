using Holmes.Intake.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Integration.NotificationHandlers;

public sealed class IntakeInviteNotificationHandler(
#pragma warning disable CS9113 // Parameter is unread - will be used when TODOs are implemented
    ISender sender,
#pragma warning restore CS9113
    ILogger<IntakeInviteNotificationHandler> logger
) : INotificationHandler<IntakeSessionInvited>
{
    public async Task Handle(IntakeSessionInvited notification, CancellationToken cancellationToken)
    {
        // TODO: Look up subject contact info (email/phone) from Subjects read model
        // TODO: Look up notification preferences from customer policy
        // TODO: Build intake URL: {baseUrl}/intake/{sessionId}?token={resumeToken}

        logger.LogDebug(
            "Intake invite notification pending for Subject {SubjectId}, Session {SessionId}, expires {ExpiresAt}",
            notification.SubjectId,
            notification.IntakeSessionId,
            notification.ExpiresAt);

        // Example of how this will work once Subject contact info is available:
        //
        // var trigger = NotificationTrigger.IntakeInvited(
        //     notification.OrderId,
        //     notification.SubjectId);
        //
        // var recipient = NotificationRecipient.Email(
        //     subjectEmail,
        //     subjectDisplayName);
        //
        // var content = new NotificationContent
        // {
        //     Subject = "Action Required: Complete Your Background Check",
        //     TemplateId = "intake-invite-v1",
        //     TemplateData = new Dictionary<string, object>
        //     {
        //         ["ResumeToken"] = notification.ResumeToken,
        //         ["IntakeUrl"] = $"{baseUrl}/intake/{notification.IntakeSessionId}?token={notification.ResumeToken}",
        //         ["ExpiresAt"] = notification.ExpiresAt.ToString("f"),
        //         ["CustomerName"] = customerName
        //     }
        // };
        //
        // await sender.Send(new CreateNotificationRequestCommand(
        //     notification.CustomerId,
        //     trigger,
        //     recipient,
        //     content,
        //     schedule: null, // Immediate
        //     NotificationPriority.High,
        //     isAdverseAction: false), cancellationToken);

        await Task.CompletedTask;
    }
}