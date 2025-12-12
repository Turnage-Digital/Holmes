using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Workflow.Infrastructure.Sql.Projections;

/// <summary>
///     Event-based projection runner for Order Summary projections.
///     Replays Order domain events to rebuild the order_summary_projections table.
/// </summary>
public sealed class OrderSummaryEventProjectionRunner : EventProjectionRunner
{
    private readonly WorkflowDbContext _workflowDbContext;

    public OrderSummaryEventProjectionRunner(
        WorkflowDbContext workflowDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<OrderSummaryEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        _workflowDbContext = workflowDbContext;
    }

    protected override string ProjectionName => "workflow.order_summary.events";

    protected override string[]? StreamTypes => ["Order"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_workflowDbContext.Database.IsRelational())
        {
            await _workflowDbContext.OrderSummaries.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _workflowDbContext.OrderSummaries.RemoveRange(_workflowDbContext.OrderSummaries);
            await _workflowDbContext.SaveChangesAsync(cancellationToken);
        }

        _workflowDbContext.ChangeTracker.Clear();
    }
}