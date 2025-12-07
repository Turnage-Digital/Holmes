using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Mappers;
using Microsoft.EntityFrameworkCore;

namespace Holmes.SlaClocks.Infrastructure.Sql;

public sealed class SlaClockRepository(SlaClockDbContext context) : ISlaClockRepository
{
    public async Task<SlaClock?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
    {
        var entity = await context.SlaClocks
            .FirstOrDefaultAsync(c => c.Id == id.ToString(), cancellationToken);

        return entity is null ? null : SlaClockMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<SlaClock>> GetByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default
    )
    {
        var entities = await context.SlaClocks
            .Where(c => c.OrderId == orderId.ToString())
            .ToListAsync(cancellationToken);

        return entities.Select(SlaClockMapper.ToDomain).ToList();
    }

    public async Task<SlaClock?> GetByOrderIdAndKindAsync(
        UlidId orderId,
        ClockKind kind,
        CancellationToken cancellationToken = default
    )
    {
        var entity = await context.SlaClocks
            .Where(c => c.OrderId == orderId.ToString() && c.Kind == (int)kind)
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : SlaClockMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<SlaClock>> GetActiveByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default
    )
    {
        var activeStates = new[] { (int)ClockState.Running, (int)ClockState.AtRisk };

        var entities = await context.SlaClocks
            .Where(c => c.OrderId == orderId.ToString() && activeStates.Contains(c.State))
            .ToListAsync(cancellationToken);

        return entities.Select(SlaClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SlaClock>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default
    )
    {
        var asOfUtc = asOf.UtcDateTime;

        var entities = await context.SlaClocks
            .Where(c => c.State == (int)ClockState.Running)
            .Where(c => c.AtRiskAt == null)
            .Where(c => c.AtRiskThresholdAt <= asOfUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(SlaClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SlaClock>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default
    )
    {
        var asOfUtc = asOf.UtcDateTime;
        var activeStates = new[] { (int)ClockState.Running, (int)ClockState.AtRisk };

        var entities = await context.SlaClocks
            .Where(c => activeStates.Contains(c.State))
            .Where(c => c.BreachedAt == null)
            .Where(c => c.DeadlineAt <= asOfUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(SlaClockMapper.ToDomain).ToList();
    }

    public void Add(SlaClock clock)
    {
        context.SlaClocks.Add(SlaClockMapper.ToDb(clock));
    }

    public void Update(SlaClock clock)
    {
        var entity = SlaClockMapper.ToDb(clock);
        context.SlaClocks.Update(entity);
    }
}