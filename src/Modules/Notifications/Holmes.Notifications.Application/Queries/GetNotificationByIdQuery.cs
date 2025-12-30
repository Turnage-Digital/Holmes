using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Notifications.Contracts.Dtos;

namespace Holmes.Notifications.Application.Queries;

public sealed record GetNotificationByIdQuery(
    string NotificationId
) : RequestBase<Result<NotificationSummaryDto>>;