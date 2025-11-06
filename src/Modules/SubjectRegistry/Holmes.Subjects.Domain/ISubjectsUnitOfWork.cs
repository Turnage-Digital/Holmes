using Holmes.Core.Domain;

namespace Holmes.Subjects.Domain;

public interface ISubjectsUnitOfWork : IUnitOfWork
{
    void RegisterDomainEvents(IHasDomainEvents aggregate);

    void RegisterDomainEvents(IEnumerable<IHasDomainEvents> aggregates);
}