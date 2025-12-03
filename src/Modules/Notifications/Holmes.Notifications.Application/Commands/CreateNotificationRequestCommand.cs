using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Domain.ValueObjects;
using MediatR;

namespace Holmes.Notifications.Application.Commands;

public sealed record CreateNotificationRequestCommand(
    UlidId CustomerId,
    NotificationTrigger Trigger,
    NotificationRecipient Recipient,
    NotificationContent Content,
    NotificationSchedule? Schedule = null,
    NotificationPriority Priority = NotificationPriority.Normal,
    bool IsAdverseAction = false,
    string? CorrelationId = null
) : RequestBase<Result<CreateNotificationRequestResult>>;

public sealed record CreateNotificationRequestResult(
    UlidId NotificationId,
    DateTimeOffset CreatedAt,
    DateTimeOffset? ScheduledFor
);

public sealed class CreateNotificationRequestCommandHandler(
    INotificationsUnitOfWork unitOfWork,
    TimeProvider timeProvider
) : IRequestHandler<CreateNotificationRequestCommand, Result<CreateNotificationRequestResult>>
{
    public async Task<Result<CreateNotificationRequestResult>> Handle(
        CreateNotificationRequestCommand request,
        CancellationToken cancellationToken)
    {
        var now = timeProvider.GetUtcNow();

        var notification = NotificationRequest.Create(
            request.CustomerId,
            request.Trigger,
            request.Recipient,
            request.Content,
            request.Schedule,
            request.Priority,
            request.IsAdverseAction,
            now,
            request.CorrelationId);

        await unitOfWork.NotificationRequests.AddAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success(new CreateNotificationRequestResult(
            notification.Id,
            notification.CreatedAt,
            notification.ScheduledFor));
    }
}
