using Holmes.Core.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Core.Infrastructure.Sql;

public abstract class UnitOfWork<TContext>(TContext dbContext, IMediator mediator, IDomainEventQueue eventQueue)
    : IUnitOfWork where TContext : DbContext
{
    private bool _disposed;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        int retval;

        // Publish any queued BeforeSave events
        foreach (var e in eventQueue.Dequeue(EventPhase.BeforeSave))
        {
            await mediator.Publish(e, cancellationToken);
        }

        // Only wrap in a transaction for relational providers
        if (dbContext.Database.IsRelational())
        {
            var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                retval = await dbContext.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                foreach (var e in eventQueue.Dequeue(EventPhase.AfterSave))
                {
                    await mediator.Publish(e, cancellationToken);
                }
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

            foreach (var e in eventQueue.Dequeue(EventPhase.AfterSave))
            {
                await mediator.Publish(e, cancellationToken);
            }
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
}