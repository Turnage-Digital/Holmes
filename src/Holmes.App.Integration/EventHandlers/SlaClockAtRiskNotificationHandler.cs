using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Integration.EventHandlers;

public sealed class SlaClockAtRiskNotificationHandler(
#pragma warning disable CS9113 // Parameter is unread - will be used when notification implementation is complete
    ISender sender,
#pragma warning restore CS9113
    ILogger<SlaClockAtRiskNotificationHandler> logger
) : INotificationHandler<SlaClockAtRisk>
{
    public async Task Handle(SlaClockAtRisk notification, CancellationToken cancellationToken)
    {
        var clockKindDisplay = notification.Kind switch
        {
            ClockKind.Intake => "Intake",
            ClockKind.Fulfillment => "Fulfillment",
            ClockKind.Overall => "Overall",
            _ => notification.Kind.ToString()
        };

        logger.LogWarning(
            "SLA clock at risk: Order {OrderId}, {ClockKind} clock {ClockId}, deadline {DeadlineAt}",
            notification.OrderId,
            clockKindDisplay,
            notification.ClockId,
            notification.DeadlineAt);

        // TODO: Look up customer notification preferences for SLA warnings
        // TODO: Look up assigned users for this order who should receive alerts
        // TODO: Build notification content based on clock kind and severity

        // Example of how this will work once customer and user info is available:
        //
        // var trigger = NotificationTrigger.SlaAtRisk(
        //     notification.OrderId,
        //     notification.Kind);
        //
        // var recipients = await GetOrderAssignees(notification.OrderId, cancellationToken);
        // foreach (var recipient in recipients)
        // {
        //     var content = new NotificationContent
        //     {
        //         Subject = $"SLA Warning: {clockKindDisplay} deadline approaching for Order {notification.OrderId}",
        //         TemplateId = "sla-at-risk-v1",
        //         TemplateData = new Dictionary<string, object>
        //         {
        //             ["OrderId"] = notification.OrderId.ToString(),
        //             ["ClockKind"] = clockKindDisplay,
        //             ["DeadlineAt"] = notification.DeadlineAt.ToString("f"),
        //             ["RemainingTime"] = (notification.DeadlineAt - DateTimeOffset.UtcNow).ToString(@"d\.hh\:mm")
        //         }
        //     };
        //
        //     await sender.Send(new CreateNotificationRequestCommand(
        //         notification.CustomerId,
        //         trigger,
        //         recipient,
        //         content,
        //         schedule: null, // Immediate
        //         NotificationPriority.High,
        //         isAdverseAction: false), cancellationToken);
        // }

        await Task.CompletedTask;
    }
}