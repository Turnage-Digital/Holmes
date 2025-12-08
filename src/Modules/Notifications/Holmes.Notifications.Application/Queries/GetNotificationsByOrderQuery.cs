using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Application.Abstractions.Dtos;
using Holmes.Notifications.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Notifications.Application.Queries;

public sealed record GetNotificationsByOrderQuery(
    UlidId OrderId
) : RequestBase<IReadOnlyList<NotificationSummaryDto>>;

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