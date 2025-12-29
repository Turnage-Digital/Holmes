using Holmes.Notifications.Application.Abstractions;
using Holmes.Notifications.Application.Abstractions.Dtos;
using Holmes.Notifications.Application.Queries;
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