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
public sealed class CustomerEventProjectionRunner(
    CustomersDbContext customersDbContext,
    CoreDbContext coreDbContext,
    IEventStore eventStore,
    IDomainEventSerializer serializer,
    IPublisher publisher,
    ILogger<CustomerEventProjectionRunner> logger
)
    : EventProjectionRunner(coreDbContext, eventStore, serializer, publisher, logger)
{
    protected override string ProjectionName => "customers.customer_projection.events";

    protected override string[]? StreamTypes => ["Customer"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (customersDbContext.Database.IsRelational())
        {
            await customersDbContext.CustomerProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            customersDbContext.CustomerProjections.RemoveRange(customersDbContext.CustomerProjections);
            await customersDbContext.SaveChangesAsync(cancellationToken);
        }

        customersDbContext.ChangeTracker.Clear();
    }
}