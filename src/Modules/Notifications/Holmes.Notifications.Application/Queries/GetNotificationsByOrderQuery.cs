using Holmes.Core.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Contracts.Dtos;

namespace Holmes.Notifications.Application.Queries;

public sealed record GetNotificationsByOrderQuery(
    UlidId OrderId
) : RequestBase<IReadOnlyList<NotificationSummaryDto>>;