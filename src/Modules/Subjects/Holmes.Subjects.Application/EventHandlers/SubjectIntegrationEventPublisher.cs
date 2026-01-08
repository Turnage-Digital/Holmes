using Holmes.Subjects.Contracts.IntegrationEvents;
using Holmes.Subjects.Domain.Events;
using MediatR;

namespace Holmes.Subjects.Application.EventHandlers;

public sealed class SubjectIntegrationEventPublisher(
    IMediator mediator
) : INotificationHandler<SubjectResolved>
{
    public Task Handle(SubjectResolved notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new SubjectResolvedIntegrationEvent(
            notification.OrderId,
            notification.CustomerId,
            notification.SubjectId,
            notification.ResolvedAt,
            notification.WasExisting), cancellationToken);
    }
}
