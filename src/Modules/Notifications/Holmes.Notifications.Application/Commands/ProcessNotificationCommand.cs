using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Notifications.Application.Commands;

public sealed record ProcessNotificationCommand(
    UlidId NotificationId
) : RequestBase<Result>;