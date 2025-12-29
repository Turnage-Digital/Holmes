using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Application.Abstractions.Dtos;

namespace Holmes.Notifications.Application.Queries;

public sealed record GetNotificationsByOrderQuery(
    UlidId OrderId
) : RequestBase<IReadOnlyList<NotificationSummaryDto>>;