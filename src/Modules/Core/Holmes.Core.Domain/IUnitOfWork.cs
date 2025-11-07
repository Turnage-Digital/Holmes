namespace Holmes.Core.Domain;

public interface IUnitOfWork : IDisposable
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    void RegisterDomainEvents(IHasDomainEvents aggregate);

    void RegisterDomainEvents(IEnumerable<IHasDomainEvents> aggregates);
}