using Holmes.Core.Domain;

namespace Holmes.Users.Domain;

public interface IUsersUnitOfWork : IUnitOfWork
{
    void RegisterDomainEvents(IHasDomainEvents aggregate);

    void RegisterDomainEvents(IEnumerable<IHasDomainEvents> aggregates);
}