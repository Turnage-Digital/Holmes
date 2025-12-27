using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;

namespace Holmes.Notifications.Application.Abstractions.Commands;

public sealed record RecordDeliveryResultCommand(
    UlidId NotificationId,
    bool Success,
    string? ProviderMessageId = null,
    string? ErrorMessage = null,
    bool IsPermanentFailure = false
) : RequestBase<Result>;