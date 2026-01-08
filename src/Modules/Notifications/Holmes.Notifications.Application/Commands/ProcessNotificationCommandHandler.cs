using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Contracts;
using Holmes.Notifications.Domain;
using Holmes.Users.Contracts;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.Notifications.Application.Commands;

public sealed class ProcessNotificationCommandHandler(
    INotificationsUnitOfWork unitOfWork,
    IEnumerable<INotificationProvider> providers,
    TimeProvider timeProvider,
    ILogger<ProcessNotificationCommandHandler> logger,
    IUserAccessQueries userAccessQueries,
    ICustomerAccessQueries customerAccessQueries
) : IRequestHandler<ProcessNotificationCommand, Result>
{
    public async Task<Result> Handle(
        ProcessNotificationCommand request,
        CancellationToken cancellationToken
    )
    {
        var notification = await unitOfWork.Notifications.GetByIdAsync(
            request.NotificationId,
            cancellationToken);

        if (notification is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        if (Ulid.TryParse(request.UserId, out var parsedActor))
        {
            var actor = UlidId.FromUlid(parsedActor);
            var isGlobalAdmin = await userAccessQueries.IsGlobalAdminAsync(actor, cancellationToken);
            if (!isGlobalAdmin)
            {
                var allowedCustomers = await customerAccessQueries.GetAdminCustomerIdsAsync(actor, cancellationToken);
                if (!allowedCustomers.Contains(notification.CustomerId.ToString()))
                {
                    return Result.Fail(ResultErrors.Forbidden);
                }
            }
        }

        if (notification.Status != DeliveryStatus.Pending &&
            notification.Status != DeliveryStatus.Failed)
        {
            logger.LogDebug(
                "Notification {NotificationId} is not processable. Status: {Status}",
                notification.Id,
                notification.Status);
            return Result.Success();
        }

        // Adverse action notifications are NOT sent by Holmes
        if (notification.IsAdverseAction)
        {
            logger.LogInformation(
                "Notification {NotificationId} is adverse action - skipping delivery. " +
                "Tenant must handle adverse action letters directly.",
                notification.Id);

            notification.MarkQueued(timeProvider.GetUtcNow());
            await unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        var provider = providers.FirstOrDefault(p => p.CanHandle(notification.Recipient.Channel));
        if (provider is null)
        {
            logger.LogWarning(
                "No provider found for channel {Channel}. Notification {NotificationId} cannot be sent.",
                notification.Recipient.Channel,
                notification.Id);

            notification.RecordDeliveryFailure(
                timeProvider.GetUtcNow(),
                $"No provider configured for channel {notification.Recipient.Channel}");

            await unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
            await unitOfWork.SaveChangesAsync(cancellationToken);
            return Result.Fail($"No provider for channel {notification.Recipient.Channel}");
        }

        notification.MarkQueued(timeProvider.GetUtcNow());

        var result = await provider.SendAsync(
            notification.Recipient,
            notification.Content,
            cancellationToken);

        var now = timeProvider.GetUtcNow();

        if (result.Success)
        {
            notification.RecordDeliverySuccess(now, result.ProviderMessageId);
            logger.LogInformation(
                "Notification {NotificationId} delivered via {Channel}. Provider ID: {ProviderId}",
                notification.Id,
                notification.Recipient.Channel,
                result.ProviderMessageId);
        }
        else if (result.ShouldRetry)
        {
            notification.RecordDeliveryFailure(now, result.ErrorMessage ?? "Unknown error", result.RetryAfter);
            logger.LogWarning(
                "Notification {NotificationId} delivery failed (will retry): {Error}",
                notification.Id,
                result.ErrorMessage);
        }
        else
        {
            notification.RecordBounce(now, result.ErrorMessage ?? "Permanent failure");
            logger.LogError(
                "Notification {NotificationId} permanently failed: {Error}",
                notification.Id,
                result.ErrorMessage);
        }

        await unitOfWork.Notifications.UpdateAsync(notification, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return result.Success
            ? Result.Success()
            : Result.Fail(result.ErrorMessage ?? "Delivery failed");
    }
}