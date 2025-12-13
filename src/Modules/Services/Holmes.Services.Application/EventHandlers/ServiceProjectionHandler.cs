using Holmes.Services.Application.Abstractions.Projections;
using Holmes.Services.Domain;
using Holmes.Services.Domain.Events;
using MediatR;

namespace Holmes.Services.Application.EventHandlers;

/// <summary>
///     Handles service request domain events to maintain the service_projections table.
/// </summary>
public sealed class ServiceProjectionHandler(
    IServiceProjectionWriter writer
)
    : INotificationHandler<ServiceRequestCreated>,
        INotificationHandler<ServiceRequestDispatched>,
        INotificationHandler<ServiceRequestInProgress>,
        INotificationHandler<ServiceRequestCompleted>,
        INotificationHandler<ServiceRequestFailed>,
        INotificationHandler<ServiceRequestCanceled>,
        INotificationHandler<ServiceRequestRetried>
{
    public Task Handle(ServiceRequestCanceled notification, CancellationToken cancellationToken)
    {
        return writer.UpdateCanceledAsync(
            notification.ServiceRequestId.ToString(),
            notification.Reason,
            notification.CanceledAt,
            cancellationToken);
    }

    public Task Handle(ServiceRequestCompleted notification, CancellationToken cancellationToken)
    {
        return writer.UpdateCompletedAsync(
            notification.ServiceRequestId.ToString(),
            notification.ResultStatus,
            notification.RecordCount,
            notification.CompletedAt,
            cancellationToken);
    }

    public Task Handle(ServiceRequestCreated notification, CancellationToken cancellationToken)
    {
        var model = new ServiceProjectionModel(
            notification.ServiceRequestId.ToString(),
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

    public Task Handle(ServiceRequestDispatched notification, CancellationToken cancellationToken)
    {
        return writer.UpdateDispatchedAsync(
            notification.ServiceRequestId.ToString(),
            notification.VendorCode,
            notification.VendorReferenceId,
            notification.DispatchedAt,
            cancellationToken);
    }

    public Task Handle(ServiceRequestFailed notification, CancellationToken cancellationToken)
    {
        return writer.UpdateFailedAsync(
            notification.ServiceRequestId.ToString(),
            notification.ErrorMessage,
            notification.AttemptCount,
            notification.WillRetry,
            notification.FailedAt,
            cancellationToken);
    }

    public Task Handle(ServiceRequestInProgress notification, CancellationToken cancellationToken)
    {
        return writer.UpdateInProgressAsync(
            notification.ServiceRequestId.ToString(),
            notification.UpdatedAt,
            cancellationToken);
    }

    public Task Handle(ServiceRequestRetried notification, CancellationToken cancellationToken)
    {
        return writer.UpdateRetriedAsync(
            notification.ServiceRequestId.ToString(),
            notification.AttemptCount,
            notification.RetriedAt,
            cancellationToken);
    }
}