using Holmes.Core.Domain;

namespace Holmes.Customers.Domain;

public interface ICustomersUnitOfWork : IUnitOfWork
{
    void RegisterDomainEvents(IHasDomainEvents aggregate);

    void RegisterDomainEvents(IEnumerable<IHasDomainEvents> aggregates);
}