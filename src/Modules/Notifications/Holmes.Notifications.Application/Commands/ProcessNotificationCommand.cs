using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Notifications.Application.Commands;

public sealed record ProcessNotificationCommand(
    UlidId NotificationId
) : RequestBase<Result>;