using Holmes.Core.Application;
using Holmes.Notifications.Contracts;
using Holmes.Notifications.Contracts.Dtos;
using MediatR;

namespace Holmes.Notifications.Application.Queries;

public sealed class GetNotificationByIdQueryHandler(
    INotificationQueries notificationQueries
) : IRequestHandler<GetNotificationByIdQuery, Result<NotificationSummaryDto>>
{
    public async Task<Result<NotificationSummaryDto>> Handle(
        GetNotificationByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var notification = await notificationQueries.GetByIdAsync(
            request.NotificationId,
            cancellationToken);

        if (notification is null)
        {
            return Result.Fail<NotificationSummaryDto>($"Notification '{request.NotificationId}' not found.");
        }

        return Result.Success(notification);
    }
}