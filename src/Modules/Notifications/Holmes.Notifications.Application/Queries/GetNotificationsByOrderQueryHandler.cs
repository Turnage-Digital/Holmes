using Holmes.Notifications.Contracts;
using Holmes.Notifications.Contracts.Dtos;
using MediatR;

namespace Holmes.Notifications.Application.Queries;

public sealed class GetNotificationsByOrderQueryHandler(
    INotificationQueries notificationQueries
) : IRequestHandler<GetNotificationsByOrderQuery, IReadOnlyList<NotificationSummaryDto>>
{
    public async Task<IReadOnlyList<NotificationSummaryDto>> Handle(
        GetNotificationsByOrderQuery request,
        CancellationToken cancellationToken
    )
    {
        return await notificationQueries.GetByOrderIdAsync(
            request.OrderId.ToString(),
            cancellationToken);
    }
}