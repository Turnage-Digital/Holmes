using Holmes.Services.Application.Abstractions;
using Holmes.Services.Domain;
using Holmes.Services.Domain.Events;
using MediatR;

namespace Holmes.Services.Application.EventHandlers;

/// <summary>
///     Handles service domain events to maintain the service_projections table.
/// </summary>
public sealed class ServiceProjectionHandler(
    IServiceProjectionWriter writer
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
        return writer.UpdateCanceledAsync(
            notification.ServiceId.ToString(),
            notification.Reason,
            notification.CanceledAt,
            cancellationToken);
    }

    public Task Handle(ServiceCompleted notification, CancellationToken cancellationToken)
    {
        return writer.UpdateCompletedAsync(
            notification.ServiceId.ToString(),
            notification.ResultStatus,
            notification.RecordCount,
            notification.CompletedAt,
            cancellationToken);
    }

    public Task Handle(ServiceCreated notification, CancellationToken cancellationToken)
    {
        var model = new ServiceProjectionModel(
            notification.ServiceId.ToString(),
            notification.OrderId.ToString(),
            notification.CustomerId.ToString(),
            notification.ServiceTypeCode,
            notification.Category,
            ServiceStatus.Pending,
            notification.Tier,
            notification.ScopeType,
            notification.ScopeValue,
            notification.CreatedAt
        );

        return writer.UpsertAsync(model, cancellationToken);
    }

    public Task Handle(ServiceDispatched notification, CancellationToken cancellationToken)
    {
        return writer.UpdateDispatchedAsync(
            notification.ServiceId.ToString(),
            notification.VendorCode,
            notification.VendorReferenceId,
            notification.DispatchedAt,
            cancellationToken);
    }

    public Task Handle(ServiceFailed notification, CancellationToken cancellationToken)
    {
        return writer.UpdateFailedAsync(
            notification.ServiceId.ToString(),
            notification.ErrorMessage,
            notification.AttemptCount,
            notification.WillRetry,
            notification.FailedAt,
            cancellationToken);
    }

    public Task Handle(ServiceInProgress notification, CancellationToken cancellationToken)
    {
        return writer.UpdateInProgressAsync(
            notification.ServiceId.ToString(),
            notification.UpdatedAt,
            cancellationToken);
    }

    public Task Handle(ServiceRetried notification, CancellationToken cancellationToken)
    {
        return writer.UpdateRetriedAsync(
            notification.ServiceId.ToString(),
            notification.AttemptCount,
            notification.RetriedAt,
            cancellationToken);
    }
}