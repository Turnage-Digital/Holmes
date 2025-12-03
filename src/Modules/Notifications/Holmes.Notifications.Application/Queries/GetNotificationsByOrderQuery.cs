using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain;
using MediatR;

namespace Holmes.Notifications.Application.Queries;

public sealed record GetNotificationsByOrderQuery(
    UlidId OrderId
) : RequestBase<IReadOnlyList<NotificationSummaryDto>>;

public sealed record NotificationSummaryDto(
    UlidId Id,
    UlidId CustomerId,
    UlidId? OrderId,
    NotificationTriggerType TriggerType,
    NotificationChannel Channel,
    string RecipientAddress,
    DeliveryStatus Status,
    bool IsAdverseAction,
    DateTimeOffset CreatedAt,
    DateTimeOffset? DeliveredAt,
    int DeliveryAttemptCount
);

public sealed class GetNotificationsByOrderQueryHandler(
    INotificationsUnitOfWork unitOfWork
) : IRequestHandler<GetNotificationsByOrderQuery, IReadOnlyList<NotificationSummaryDto>>
{
    public async Task<IReadOnlyList<NotificationSummaryDto>> Handle(
        GetNotificationsByOrderQuery request,
        CancellationToken cancellationToken
    )
    {
        var notifications = await unitOfWork.NotificationRequests.GetByOrderIdAsync(
            request.OrderId,
            cancellationToken);

        return notifications
            .Select(n => new NotificationSummaryDto(
                n.Id,
                n.CustomerId,
                n.OrderId,
                n.TriggerType,
                n.Recipient.Channel,
                n.Recipient.Address,
                n.Status,
                n.IsAdverseAction,
                n.CreatedAt,
                n.DeliveredAt,
                n.DeliveryAttempts.Count))
            .ToList();
    }
}