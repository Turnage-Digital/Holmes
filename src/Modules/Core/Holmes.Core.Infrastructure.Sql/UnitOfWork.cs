using System.Diagnostics;
using Holmes.Core.Application.Abstractions;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Core.Infrastructure.Sql;

public abstract class UnitOfWork<TContext>(
    TContext dbContext,
    IMediator mediator,
    IEventStore? eventStore = null,
    IDomainEventSerializer? serializer = null,
    ITenantContext? tenantContext = null)
    : IUnitOfWork where TContext : DbContext
{
    private bool _disposed;

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken)
    {
        using var activity = UnitOfWorkTelemetry.ActivitySource.StartActivity();
        var tags = new TagList
        {
            { "db.context", typeof(TContext).Name },
            { "db.provider", dbContext.Database.ProviderName ?? "unknown" },
            { "db.transactional", dbContext.Database.IsRelational() }
        };
        activity?.SetTag("db.system", dbContext.Database.ProviderName);
        activity?.SetTag("holmes.unit_of_work.context", typeof(TContext).Name);

        var startTimestamp = Stopwatch.GetTimestamp();

        // Collect aggregates before any database operations
        var aggregates = DomainEventTracker.Collect();

        try
        {
            int retval;

            // Only wrap in a transaction for relational providers
            if (dbContext.Database.IsRelational())
            {
                await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
                try
                {
                    // 1. Save aggregate state changes
                    retval = await dbContext.SaveChangesAsync(cancellationToken);

                    // 2. Persist events to EventRecord (within transaction)
                    await PersistEventsAsync(aggregates, cancellationToken);

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

                // Still persist events for non-relational (testing) scenarios
                await PersistEventsAsync(aggregates, cancellationToken);
            }

            // 3. Dispatch events via MediatR (after commit)
            await DispatchDomainEventsAsync(aggregates, cancellationToken);

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

    private async Task PersistEventsAsync(
        IReadOnlyCollection<IHasDomainEvents> aggregates,
        CancellationToken cancellationToken)
    {
        // Skip if event persistence infrastructure is not configured
        if (eventStore is null || serializer is null)
        {
            return;
        }

        var tenant = tenantContext?.TenantId ?? "*";
        var actorId = tenantContext?.ActorId;
        var correlationId = Activity.Current?.TraceId.ToString();
        var causationId = Activity.Current?.ParentSpanId.ToString();

        foreach (var aggregate in aggregates.OfType<AggregateRoot>())
        {
            if (aggregate.DomainEvents.Count == 0) continue;

            var envelopes = aggregate.DomainEvents
                .Select(e => serializer.Serialize(e, correlationId, causationId, actorId))
                .ToList();

            await eventStore.AppendEventsAsync(
                tenant,
                aggregate.GetStreamId(),
                aggregate.GetStreamType(),
                envelopes,
                cancellationToken);
        }
    }

    private async Task DispatchDomainEventsAsync(
        IReadOnlyCollection<IHasDomainEvents> aggregates,
        CancellationToken cancellationToken)
    {
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