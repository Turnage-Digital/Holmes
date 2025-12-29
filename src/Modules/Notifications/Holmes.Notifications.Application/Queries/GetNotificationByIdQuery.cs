using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Notifications.Application.Abstractions.Dtos;

namespace Holmes.Notifications.Application.Queries;

public sealed record GetNotificationByIdQuery(
    string NotificationId
) : RequestBase<Result<NotificationSummaryDto>>;