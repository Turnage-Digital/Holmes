using System.Diagnostics;
using System.Diagnostics.Metrics;
using Holmes.Core.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Core.Infrastructure.Sql;

public abstract class UnitOfWork<TContext>(TContext dbContext, IMediator mediator)
    : IUnitOfWork where TContext : DbContext
{
    private bool _disposed;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        using var activity = UnitOfWorkTelemetry.ActivitySource.StartActivity(
            "UnitOfWork.SaveChanges",
            ActivityKind.Internal);
        var tags = new TagList
        {
            { "db.context", typeof(TContext).Name },
            { "db.provider", dbContext.Database.ProviderName ?? "unknown" },
            { "db.transactional", dbContext.Database.IsRelational() }
        };
        activity?.SetTag("db.system", dbContext.Database.ProviderName);
        activity?.SetTag("holmes.unit_of_work.context", typeof(TContext).Name);

        var startTimestamp = Stopwatch.GetTimestamp();

        try
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
            activity?.SetStatus(ActivityStatusCode.Ok);
            activity?.SetTag("holmes.unit_of_work.result", "success");

            return retval;
        }
        catch (Exception ex)
        {
            UnitOfWorkTelemetry.SaveChangesFailures.Add(1, tags);
            activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
            activity?.SetTag("exception.type", ex.GetType().FullName);
            activity?.SetTag("exception.message", ex.Message);
            activity?.SetTag("exception.stacktrace", ex.StackTrace);
            activity?.SetTag("holmes.unit_of_work.result", "failure");
            throw;
        }
        finally
        {
            var duration = Stopwatch.GetElapsedTime(startTimestamp).TotalMilliseconds;
            UnitOfWorkTelemetry.SaveChangesDuration.Record(duration, tags);
            activity?.SetTag("holmes.unit_of_work.duration_ms", duration);
        }
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
