using MediatR;

namespace Holmes.Core.Domain;

public interface IDomainEventQueue
{
    void Enqueue(INotification @event, EventPhase phase);
    IReadOnlyCollection<INotification> Dequeue(EventPhase phase);
}