using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Notifications.Application.Abstractions.Dtos;

namespace Holmes.Notifications.Application.Abstractions.Queries;

public sealed record GetNotificationByIdQuery(
    string NotificationId
) : RequestBase<Result<NotificationSummaryDto>>;
