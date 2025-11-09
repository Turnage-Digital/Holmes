using System.Collections.ObjectModel;
using MediatR;

namespace Holmes.Core.Domain;

public abstract class AggregateRoot : IHasDomainEvents
{
    private readonly List<INotification> _domainEvents = [];

    public IReadOnlyCollection<INotification> DomainEvents => new ReadOnlyCollection<INotification>(_domainEvents);

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    protected void AddDomainEvent(INotification domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);

        _domainEvents.Add(domainEvent);
        DomainEventTracker.Register(this);
    }
}
