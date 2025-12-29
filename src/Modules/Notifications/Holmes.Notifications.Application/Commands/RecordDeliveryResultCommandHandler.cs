using Holmes.Core.Domain;
using Holmes.Notifications.Application.Commands;
using Holmes.Notifications.Domain;
using MediatR;

namespace Holmes.Notifications.Application.Commands;

public sealed class RecordDeliveryResultCommandHandler(
    INotificationsUnitOfWork unitOfWork,
    TimeProvider timeProvider
) : IRequestHandler<RecordDeliveryResultCommand, Result>
{
    public async Task<Result> Handle(
        RecordDeliveryResultCommand request,
        CancellationToken cancellationToken
    )
    {
        var notification = await unitOfWork.Notifications.GetByIdAsync(
            request.NotificationId,
            cancellationToken);

        if (notification is null)
        {
            return Result.Fail($"Notification {request.NotificationId} not found.");
        }

        var now = timeProvider.GetUtcNow();

        if (request.Success)
        {
            notification.RecordDeliverySuccess(now, request.ProviderMessageId);
        }
        else if (request.IsPermanentFailure)
        {
            notification.RecordBounce(now, request.ErrorMessage ?? "Permanent failure");
        }
        else
        {
            notification.RecordDeliveryFailure(now, request.ErrorMessage ?? "Delivery failed");
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}