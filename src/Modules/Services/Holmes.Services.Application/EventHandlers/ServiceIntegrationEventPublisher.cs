using Holmes.Services.Application.Abstractions.IntegrationEvents;
using Holmes.Services.Domain.Events;
using MediatR;

namespace Holmes.Services.Application.EventHandlers;

public sealed class ServiceIntegrationEventPublisher(
    IMediator mediator
) : INotificationHandler<ServiceCompleted>
{
    public Task Handle(ServiceCompleted notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new ServiceCompletedIntegrationEvent(
            notification.ServiceId,
            notification.OrderId,
            notification.CustomerId,
            notification.ServiceTypeCode,
            notification.ResultStatus,
            notification.RecordCount,
            notification.CompletedAt), cancellationToken);
    }
}