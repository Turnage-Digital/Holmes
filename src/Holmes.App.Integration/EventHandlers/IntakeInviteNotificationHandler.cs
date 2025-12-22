using Holmes.Customers.Application.Abstractions.Queries;
using Holmes.Intake.Domain.Events;
using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.ValueObjects;
using Holmes.Subjects.Application.Abstractions.Queries;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Integration.EventHandlers;

public sealed class IntakeInviteNotificationHandler(
    ISender sender,
    ISubjectQueries subjectQueries,
    ICustomerQueries customerQueries,
    IConfiguration configuration,
    ILogger<IntakeInviteNotificationHandler> logger
) : INotificationHandler<IntakeSessionInvited>
{
    public async Task Handle(IntakeSessionInvited notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Processing intake invite notification for Subject {SubjectId}, Session {SessionId}, expires {ExpiresAt}",
            notification.SubjectId,
            notification.IntakeSessionId,
            notification.ExpiresAt);

        // Look up subject contact info from read model
        var subject = await subjectQueries.GetSummaryByIdAsync(
            notification.SubjectId.ToString(),
            cancellationToken);

        if (subject is null)
        {
            logger.LogWarning(
                "Cannot send intake invite: Subject {SubjectId} not found",
                notification.SubjectId);
            return;
        }

        if (string.IsNullOrWhiteSpace(subject.Email))
        {
            logger.LogWarning(
                "Cannot send intake invite: Subject {SubjectId} has no email address",
                notification.SubjectId);
            return;
        }

        // Look up customer name for the notification
        var customer = await customerQueries.GetListItemByIdAsync(
            notification.CustomerId.ToString(),
            cancellationToken);

        var customerName = customer?.Name ?? "Your employer";

        // Build intake URL
        var baseUrl = configuration["Intake:BaseUrl"]?.TrimEnd('/') ?? "https://localhost:5003";
        var intakeUrl = $"{baseUrl}/intake/{notification.IntakeSessionId}?token={notification.ResumeToken}";

        // Build subject display name
        var subjectDisplayName = $"{subject.GivenName} {subject.FamilyName}".Trim();
        if (string.IsNullOrEmpty(subjectDisplayName))
        {
            subjectDisplayName = subject.Email;
        }

        // Create notification trigger
        var trigger = NotificationTrigger.IntakeInvited(
            notification.OrderId,
            notification.SubjectId,
            notification.CustomerId);

        // Create recipient
        var recipient = NotificationRecipient.Email(
            subject.Email,
            subjectDisplayName);

        // Create notification content with template
        var content = new NotificationContent
        {
            Subject = "Action Required: Complete Your Background Check",
            TemplateId = "intake-invite-v1",
            Body = BuildPlainTextBody(subjectDisplayName, customerName, intakeUrl, notification.ExpiresAt),
            TemplateData = new Dictionary<string, object>
            {
                ["SubjectName"] = subjectDisplayName,
                ["CustomerName"] = customerName,
                ["IntakeUrl"] = intakeUrl,
                ["ExpiresAt"] = notification.ExpiresAt.ToString("f"),
                ["ExpiresAtUtc"] = notification.ExpiresAt.UtcDateTime.ToString("O")
            }
        };

        // Send the notification request
        var result = await sender.Send(
            new CreateNotificationRequestCommand(
                notification.CustomerId,
                trigger,
                recipient,
                content,
                null, // Immediate
                NotificationPriority.High),
            cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Intake invite notification created for Subject {SubjectId} ({Email}), Session {SessionId}, NotificationId {NotificationId}",
                notification.SubjectId,
                subject.Email,
                notification.IntakeSessionId,
                result.Value?.NotificationId);
        }
        else
        {
            logger.LogError(
                "Failed to create intake invite notification for Subject {SubjectId}: {Error}",
                notification.SubjectId,
                result.Error);
        }
    }

    private static string BuildPlainTextBody(
        string subjectName,
        string customerName,
        string intakeUrl,
        DateTimeOffset expiresAt
    )
    {
        return $"""
                Hello {subjectName},

                {customerName} has requested a background check as part of your application process.

                Please complete the required intake form by clicking the link below:

                {intakeUrl}

                This link will expire on {expiresAt:f}.

                If you have any questions, please contact {customerName} directly.

                Thank you,
                Holmes Background Screening
                """;
    }
}