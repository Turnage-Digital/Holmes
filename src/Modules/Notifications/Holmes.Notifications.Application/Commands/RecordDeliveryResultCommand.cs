using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Notifications.Domain;
using MediatR;

namespace Holmes.Notifications.Application.Commands;

public sealed record RecordDeliveryResultCommand(
    UlidId NotificationId,
    bool Success,
    string? ProviderMessageId = null,
    string? ErrorMessage = null,
    bool IsPermanentFailure = false
) : RequestBase<Result>;

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
        var notification = await unitOfWork.NotificationRequests.GetByIdAsync(
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