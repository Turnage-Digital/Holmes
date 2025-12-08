using Holmes.Core.Domain;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Infrastructure.Sql;

public class ServicesUnitOfWork : IServicesUnitOfWork
{
    private readonly ServicesDbContext _context;
    private readonly IMediator _mediator;
    private bool _disposed;
    private ServiceRequestRepository? _serviceRequests;

    public ServicesUnitOfWork(ServicesDbContext context, IMediator mediator)
    {
        _context = context;
        _mediator = mediator;
    }

    public IServiceRequestRepository ServiceRequests =>
        _serviceRequests ??= new ServiceRequestRepository(_context);

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Dispatch domain events before saving
        var aggregates = _context.ChangeTracker
            .Entries<AggregateRoot>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var domainEvents = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        // Clear events before dispatching to avoid re-dispatching on retry
        foreach (var aggregate in aggregates)
        {
            aggregate.ClearDomainEvents();
        }

        // Save changes
        var result = await _context.SaveChangesAsync(cancellationToken);

        // Dispatch events after successful save
        foreach (var domainEvent in domainEvents)
        {
            await _mediator.Publish(domainEvent, cancellationToken);
        }

        return result;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                _context.Dispose();
            }

            _disposed = true;
        }
    }
}