using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Notifications.Application.Abstractions.Commands;

public sealed record ProcessNotificationCommand(
    UlidId NotificationId
) : RequestBase<Result>;