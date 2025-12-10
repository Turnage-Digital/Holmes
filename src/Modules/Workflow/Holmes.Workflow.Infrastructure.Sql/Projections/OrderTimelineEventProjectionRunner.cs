using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Workflow.Infrastructure.Sql.Projections;

/// <summary>
///     Event-based projection runner for Order Timeline projections.
///     Replays Order and IntakeSession domain events to rebuild the order_timeline_projections table.
/// </summary>
public sealed class OrderTimelineEventProjectionRunner : EventProjectionRunner
{
    private readonly WorkflowDbContext _workflowDbContext;

    public OrderTimelineEventProjectionRunner(
        WorkflowDbContext workflowDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<OrderTimelineEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        _workflowDbContext = workflowDbContext;
    }

    protected override string ProjectionName => "workflow.order_timeline.events";

    // Timeline includes both Order and IntakeSession events
    protected override string[]? StreamTypes => ["Order", "IntakeSession"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_workflowDbContext.Database.IsRelational())
        {
            await _workflowDbContext.OrderTimelineEvents.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _workflowDbContext.OrderTimelineEvents.RemoveRange(_workflowDbContext.OrderTimelineEvents);
            await _workflowDbContext.SaveChangesAsync(cancellationToken);
        }

        _workflowDbContext.ChangeTracker.Clear();
    }
}