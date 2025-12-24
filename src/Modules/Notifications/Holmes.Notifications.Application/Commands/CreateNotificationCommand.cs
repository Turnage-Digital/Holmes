using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.ValueObjects;
using MediatR;

namespace Holmes.Notifications.Application.Commands;

public sealed record CreateNotificationCommand(
    UlidId CustomerId,
    NotificationTrigger Trigger,
    NotificationRecipient Recipient,
    NotificationContent Content,
    NotificationSchedule? Schedule = null,
    NotificationPriority Priority = NotificationPriority.Normal,
    bool IsAdverseAction = false,
    string? CorrelationId = null
) : RequestBase<Result<CreateNotificationResult>>, ISkipUserAssignment;

public sealed record CreateNotificationResult(
    UlidId NotificationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor
);

public sealed class CreateNotificationCommandHandler(
    INotificationsUnitOfWork unitOfWork,
    TimeProvider timeProvider
) : IRequestHandler<CreateNotificationCommand, Result<CreateNotificationResult>>
{
    public async Task<Result<CreateNotificationResult>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken
    )
    {
        var now = timeProvider.GetUtcNow();

        var notification = Notification.Create(
            request.CustomerId,
            request.Trigger,
            request.Recipient,
            request.Content,
            request.Schedule,
            request.Priority,
            request.IsAdverseAction,
            now,
            request.CorrelationId);

        await unitOfWork.Notifications.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateNotificationResult(
            notification.Id,
            notification.CreatedAt,
            notification.ScheduledFor));
    }
}