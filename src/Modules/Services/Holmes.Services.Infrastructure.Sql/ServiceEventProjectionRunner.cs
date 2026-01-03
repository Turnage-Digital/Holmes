using Holmes.Core.Contracts.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Services.Infrastructure.Sql;

/// <summary>
///     Event-based projection runner for Service Request projections.
///     Replays Service domain events to rebuild the service_projections table.
/// </summary>
public sealed class ServiceEventProjectionRunner : EventProjectionRunner
{
    private readonly ServicesDbContext _servicesDbContext;

    public ServiceEventProjectionRunner(
        ServicesDbContext servicesDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<ServiceEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        _servicesDbContext = servicesDbContext;
    }

    protected override string ProjectionName => "services.service_projection.events";

    protected override string[]? StreamTypes => ["Service"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_servicesDbContext.Database.IsRelational())
        {
            await _servicesDbContext.ServiceProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _servicesDbContext.ServiceProjections.RemoveRange(_servicesDbContext.ServiceProjections);
            await _servicesDbContext.SaveChangesAsync(cancellationToken);
        }

        _servicesDbContext.ChangeTracker.Clear();
    }
}