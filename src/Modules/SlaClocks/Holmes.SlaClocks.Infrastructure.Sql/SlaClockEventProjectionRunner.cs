using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Projections;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.SlaClocks.Infrastructure.Sql;

/// <summary>
///     Event-based projection runner for SLA Clock projections.
///     Replays SlaClock domain events to rebuild the sla_clock_projections table.
/// </summary>
public sealed class SlaClockEventProjectionRunner : EventProjectionRunner
{
    private readonly SlaClocksDbContext _slaClockDbContext;

    public SlaClockEventProjectionRunner(
        SlaClocksDbContext slaClockDbContext,
        CoreDbContext coreDbContext,
        IEventStore eventStore,
        IDomainEventSerializer serializer,
        IPublisher publisher,
        ILogger<SlaClockEventProjectionRunner> logger
    )
        : base(coreDbContext, eventStore, serializer, publisher, logger)
    {
        _slaClockDbContext = slaClockDbContext;
    }

    protected override string ProjectionName => "slaclocks.sla_clock_projection.events";

    protected override string[]? StreamTypes => ["SlaClock"];

    protected override async Task ResetProjectionAsync(CancellationToken cancellationToken)
    {
        if (_slaClockDbContext.Database.IsRelational())
        {
            await _slaClockDbContext.SlaClockProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            _slaClockDbContext.SlaClockProjections.RemoveRange(_slaClockDbContext.SlaClockProjections);
            await _slaClockDbContext.SaveChangesAsync(cancellationToken);
        }

        _slaClockDbContext.ChangeTracker.Clear();
    }
}