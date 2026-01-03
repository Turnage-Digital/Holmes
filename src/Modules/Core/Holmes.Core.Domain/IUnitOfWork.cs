namespace Holmes.Core.Domain;

public interface IUnitOfWork : IDisposable
{
    /// <summary>
    ///     Saves all pending changes and dispatches domain events.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    ///     Saves all pending changes. When deferDispatch is true, events are
    ///     persisted to the outbox but not dispatched - the OutboxProcessor
    ///     will dispatch them after the transaction commits.
    /// </summary>
    Task<int> SaveChangesAsync(bool deferDispatch, CancellationToken cancellationToken);
}