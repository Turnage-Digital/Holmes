using MediatR;

namespace Holmes.Core.Domain;

public interface IHasDomainEvents
{
    IReadOnlyCollection<INotification> DomainEvents { get; }

    void ClearDomainEvents();
}