using System.Collections.ObjectModel;
using MediatR;

namespace Holmes.Core.Domain;

public abstract class AggregateRoot : IHasDomainEvents
{
    private readonly List<INotification> _domainEvents = [];

    public IReadOnlyCollection<INotification> DomainEvents => new ReadOnlyCollection<INotification>(_domainEvents);

    /// <summary>
    /// Gets the unique identifier for this aggregate's event stream.
    /// Format: "{StreamType}:{AggregateId}" (e.g., "Order:01ARZ3NDEKTSV4RRFFQ69G5FAV")
    /// </summary>
    public abstract string GetStreamId();

    /// <summary>
    /// Gets the type name for this aggregate's event stream.
    /// Used for filtering events during projection replay.
    /// </summary>
    public abstract string GetStreamType();

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