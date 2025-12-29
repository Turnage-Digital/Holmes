using Holmes.SlaClocks.Application.Abstractions.IntegrationEvents;
using Holmes.SlaClocks.Domain.Events;
using MediatR;

namespace Holmes.SlaClocks.Application.EventHandlers;

public sealed class SlaClockIntegrationEventPublisher(
    IMediator mediator
) : INotificationHandler<SlaClockAtRisk>,
    INotificationHandler<SlaClockBreached>
{
    public Task Handle(SlaClockAtRisk notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new SlaClockAtRiskIntegrationEvent(
            notification.ClockId,
            notification.OrderId,
            notification.CustomerId,
            notification.Kind,
            notification.AtRiskAt,
            notification.DeadlineAt), cancellationToken);
    }

    public Task Handle(SlaClockBreached notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new SlaClockBreachedIntegrationEvent(
            notification.ClockId,
            notification.OrderId,
            notification.CustomerId,
            notification.Kind,
            notification.BreachedAt,
            notification.DeadlineAt), cancellationToken);
    }
}