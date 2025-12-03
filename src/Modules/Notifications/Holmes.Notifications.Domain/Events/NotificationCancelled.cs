using Holmes.Core.Domain.ValueObjects;
using MediatR;

namespace Holmes.Notifications.Domain.Events;

public sealed record NotificationCancelled(
    UlidId NotificationId,
    DateTimeOffset CancelledAt,
    string Reason
) : INotification;