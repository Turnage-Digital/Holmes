using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Notifications.Application.Abstractions.Dtos;
using Holmes.Notifications.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Notifications.Application.Queries;

public sealed record GetNotificationByIdQuery(
    string NotificationId
) : RequestBase<Result<NotificationSummaryDto>>;

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