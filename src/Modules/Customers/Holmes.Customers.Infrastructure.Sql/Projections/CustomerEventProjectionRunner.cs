using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Customers.Infrastructure.Sql.Projections;

/// <summary>
///     Event-based projection runner for Customer projections.
///     Replays Customer domain events to rebuild the customer_projections table.
/// </summary>
public sealed class CustomerEventProjectionRunner : EventProjectionRunner
{
    private readonly CustomersDbContext _customersDbContext;

    public CustomerEventProjectionRunner(
        CustomersDbContext customersDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<CustomerEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        _customersDbContext = customersDbContext;
    }

    protected override string ProjectionName => "customers.customer_projection.events";

    protected override string[]? StreamTypes => ["Customer"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_customersDbContext.Database.IsRelational())
        {
            await _customersDbContext.CustomerProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _customersDbContext.CustomerProjections.RemoveRange(_customersDbContext.CustomerProjections);
            await _customersDbContext.SaveChangesAsync(cancellationToken);
        }

        _customersDbContext.ChangeTracker.Clear();
    }
}