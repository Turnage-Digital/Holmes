using Holmes.Core.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Core.Infrastructure.Sql;

public abstract class UnitOfWork<TContext>(TContext dbContext, IMediator mediator)
    : IUnitOfWork where TContext : DbContext
{
    private readonly List<INotification> _domainEvents = [];
    private bool _disposed;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        int retval;

        // Only wrap in a transaction for relational providers
        if (dbContext.Database.IsRelational())
        {
            var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                retval = await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                await DispatchDomainEventsAsync(mediator, cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken)!;
                throw;
            }
            finally
            {
                transaction.Dispose();
            }
        }
        else
        {
            // InMemory and other non-relational providers: no transaction
            retval = await dbContext.SaveChangesAsync(cancellationToken);
            await DispatchDomainEventsAsync(mediator, cancellationToken);
        }

        return retval;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                dbContext.Dispose();
            }
        }

        _disposed = true;
    }

    public void RegisterDomainEvents(IHasDomainEvents aggregate)
    {
        if (aggregate is null)
        {
            return;
        }

        if (aggregate.DomainEvents.Count == 0)
        {
            return;
        }

        _domainEvents.AddRange(aggregate.DomainEvents);
        aggregate.ClearDomainEvents();
    }

    public void RegisterDomainEvents(IEnumerable<IHasDomainEvents> aggregates)
    {
        foreach (var aggregate in aggregates)
        {
            RegisterDomainEvents(aggregate);
        }
    }

    private async Task DispatchDomainEventsAsync(IMediator mediator, CancellationToken cancellationToken)
    {
        if (_domainEvents.Count == 0)
        {
            return;
        }

        var events = _domainEvents.ToArray();
        _domainEvents.Clear();

        foreach (var notification in events)
        {
            await mediator.Publish(notification, cancellationToken);
        }
    }
}
