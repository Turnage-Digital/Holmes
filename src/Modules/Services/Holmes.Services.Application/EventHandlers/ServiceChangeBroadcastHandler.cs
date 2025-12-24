using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions;
using Holmes.Services.Domain;
using Holmes.Services.Domain.Events;
using MediatR;

namespace Holmes.Services.Application.EventHandlers;

/// <summary>
///     Handles service domain events to broadcast real-time updates via SSE.
/// </summary>
public sealed class ServiceChangeBroadcastHandler(
    IServiceChangeBroadcaster broadcaster
)
    : INotificationHandler<ServiceCreated>,
        INotificationHandler<ServiceDispatched>,
        INotificationHandler<ServiceInProgress>,
        INotificationHandler<ServiceCompleted>,
        INotificationHandler<ServiceFailed>,
        INotificationHandler<ServiceCanceled>,
        INotificationHandler<ServiceRetried>
{
    public Task Handle(ServiceCanceled notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Canceled,
            notification.Reason,
            notification.CanceledAt), cancellationToken);
    }

    public Task Handle(ServiceCompleted notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Completed,
            $"{notification.ResultStatus}: {notification.RecordCount} record(s)",
            notification.CompletedAt), cancellationToken);
    }

    public Task Handle(ServiceCreated notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Pending,
            null,
            notification.CreatedAt), cancellationToken);
    }

    public Task Handle(ServiceDispatched notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Dispatched,
            $"Dispatched to {notification.VendorCode}",
            notification.DispatchedAt), cancellationToken);
    }

    public Task Handle(ServiceFailed notification, CancellationToken cancellationToken)
    {
        var reason = notification.WillRetry
            ? $"Failed (attempt {notification.AttemptCount}/{notification.MaxAttempts}): {notification.ErrorMessage}"
            : $"Failed: {notification.ErrorMessage}";

        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Failed,
            reason,
            notification.FailedAt), cancellationToken);
    }

    public Task Handle(ServiceInProgress notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.InProgress,
            null,
            notification.UpdatedAt), cancellationToken);
    }

    public Task Handle(ServiceRetried notification, CancellationToken cancellationToken)
    {
        return broadcaster.PublishAsync(new ServiceChange(
            UlidId.NewUlid(),
            notification.ServiceId,
            notification.OrderId,
            notification.ServiceTypeCode,
            ServiceStatus.Pending,
            $"Retry attempt {notification.AttemptCount}",
            notification.RetriedAt), cancellationToken);
    }
}