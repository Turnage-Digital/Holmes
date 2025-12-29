using Holmes.Subjects.Application.Abstractions.IntegrationEvents;
using Holmes.Subjects.Domain.Events;
using MediatR;

namespace Holmes.Subjects.Application.EventHandlers;

public sealed class SubjectIntegrationEventPublisher(
    IMediator mediator
) : INotificationHandler<SubjectIntakeRequested>
{
    public Task Handle(SubjectIntakeRequested notification, CancellationToken cancellationToken)
    {
        return mediator.Publish(new SubjectIntakeRequestedIntegrationEvent(
            notification.OrderId,
            notification.SubjectId,
            notification.CustomerId,
            notification.PolicySnapshotId,
            notification.RequestedAt,
            notification.RequestedBy), cancellationToken);
    }
}