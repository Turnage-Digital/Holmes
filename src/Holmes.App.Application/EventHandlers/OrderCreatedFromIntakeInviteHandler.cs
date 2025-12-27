using Holmes.IntakeSessions.Application.Abstractions.Commands;
using Holmes.Orders.Domain.Events;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Application.EventHandlers;

/// <summary>
///     Issues an intake invite for orders created from subject intake requests.
/// </summary>
public sealed class OrderCreatedFromIntakeInviteHandler(
    ISender sender,
    ILogger<OrderCreatedFromIntakeInviteHandler> logger
) : INotificationHandler<OrderCreatedFromIntake>
{
    private static readonly TimeSpan DefaultTimeToLive = TimeSpan.FromHours(168);

    public async Task Handle(OrderCreatedFromIntake notification, CancellationToken cancellationToken)
    {
        var command = new IssueIntakeInviteCommand(
            notification.OrderId,
            notification.SubjectId,
            notification.CustomerId,
            notification.PolicySnapshotId,
            "v1",
            new Dictionary<string, string>(),
            null,
            notification.CreatedAt,
            notification.CreatedAt,
            DefaultTimeToLive,
            null)
        {
            UserId = notification.RequestedBy.ToString()
        };

        var result = await sender.Send(command, cancellationToken);
        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Issued intake invite for Order {OrderId}, Session {SessionId}",
                notification.OrderId,
                result.Value.IntakeSessionId);
        }
        else
        {
            logger.LogWarning(
                "Failed to issue intake invite for Order {OrderId}: {Error}",
                notification.OrderId,
                result.Error);
        }
    }
}
