using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Notifications.Contracts.Dtos;

namespace Holmes.Notifications.Application.Queries;

public sealed record GetNotificationByIdQuery(
    string NotificationId
) : RequestBase<Result<NotificationSummaryDto>>;