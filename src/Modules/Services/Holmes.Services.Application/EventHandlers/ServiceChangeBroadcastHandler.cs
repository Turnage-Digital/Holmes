using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions;
using Holmes.Services.Domain;
using Holmes.Services.Domain.Events;
using MediatR;

namespace Holmes.Services.Application.EventHandlers;

/// <summary>
///     Handles service request domain events to broadcast real-time updates via SSE.
/// </summary>
public sealed class ServiceChangeBroadcastHandler(
    IServiceChangeBroadcaster broadcaster
)
    : INotificationHandler<ServiceRequestCreated>,
        INotificationHandler<ServiceRequestDispatched>,
        INotificationHandler<ServiceRequestInProgress>,
        INotificationHandler<ServiceRequestCompleted>,
        INotificationHandler<ServiceRequestFailed>,
        INotificationHandler<ServiceRequestCanceled>,
        INotificationHandler<ServiceRequestRetried>
{
    public Task Handle(ServiceRequestCreated notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceRequestId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Pending,
            null,
            notification.CreatedAt), cancellationToken);
    }

    public Task Handle(ServiceRequestDispatched notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceRequestId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Dispatched,
            $"Dispatched to {notification.VendorCode}",
            notification.DispatchedAt), cancellationToken);
    }

    public Task Handle(ServiceRequestInProgress notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceRequestId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.InProgress,
            null,
            notification.UpdatedAt), cancellationToken);
    }

    public Task Handle(ServiceRequestCompleted notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceRequestId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Completed,
            $"{notification.ResultStatus}: {notification.RecordCount} record(s)",
            notification.CompletedAt), cancellationToken);
    }

    public Task Handle(ServiceRequestFailed notification, CancellationToken cancellationToken)
    {
        var reason = notification.WillRetry
            ? $"Failed (attempt {notification.AttemptCount}/{notification.MaxAttempts}): {notification.ErrorMessage}"
            : $"Failed: {notification.ErrorMessage}";

        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceRequestId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Failed,
            reason,
            notification.FailedAt), cancellationToken);
    }

    public Task Handle(ServiceRequestCanceled notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceRequestId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Canceled,
            notification.Reason,
            notification.CanceledAt), cancellationToken);
    }

    public Task Handle(ServiceRequestRetried notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceRequestId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Pending,
            $"Retry attempt {notification.AttemptCount}",
            notification.RetriedAt), cancellationToken);
    }
}
