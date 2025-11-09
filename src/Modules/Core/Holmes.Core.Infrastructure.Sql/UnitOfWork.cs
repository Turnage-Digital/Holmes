using Holmes.Core.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Holmes.Core.Infrastructure.Sql;

public abstract class UnitOfWork<TContext>(TContext dbContext, IMediator mediator)
    : IUnitOfWork where TContext : DbContext
{
    private bool _disposed;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        int retval;

        // Only wrap in a transaction for relational providers
        if (dbContext.Database.IsRelational())
        {
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                retval = await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken)!;
                throw;
            }
        }
        else
        {
            // InMemory and other non-relational providers: no transaction
            retval = await dbContext.SaveChangesAsync(cancellationToken);
        }

        await DispatchDomainEventsAsync(cancellationToken);

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

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var aggregates = DomainEventTracker.Collect();
        if (aggregates.Count == 0)
        {
            return;
        }

        var events = aggregates.SelectMany(a => a.DomainEvents).ToArray();
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        foreach (var notification in events)
        {
            await mediator.Publish(notification, cancellationToken);
        }
    }
}
