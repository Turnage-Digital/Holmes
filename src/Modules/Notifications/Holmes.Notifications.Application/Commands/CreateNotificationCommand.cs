using Holmes.Core.Domain;
using Holmes.Notifications.Application.Abstractions.Commands;
using Holmes.Notifications.Domain;
using MediatR;

namespace Holmes.Notifications.Application.Commands;

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