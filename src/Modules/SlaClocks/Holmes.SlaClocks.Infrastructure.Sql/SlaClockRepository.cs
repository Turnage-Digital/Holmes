using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.SlaClocks.Infrastructure.Sql;

public sealed class SlaClockRepository(SlaClockDbContext context) : ISlaClockRepository
{
    public async Task<SlaClock?> GetByIdAsync(UlidId id, CancellationToken cancellationToken = default)
    {
        var entity = await context.SlaClocks
            .FirstOrDefaultAsync(c => c.Id == id.ToString(), cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<SlaClock>> GetByOrderIdAsync(UlidId orderId, CancellationToken cancellationToken = default)
    {
        var entities = await context.SlaClocks
            .Where(c => c.OrderId == orderId.ToString())
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<SlaClock?> GetByOrderIdAndKindAsync(
        UlidId orderId,
        ClockKind kind,
        CancellationToken cancellationToken = default)
    {
        var entity = await context.SlaClocks
            .Where(c => c.OrderId == orderId.ToString() && c.Kind == (int)kind)
            .OrderByDescending(c => c.StartedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : ToDomain(entity);
    }

    public async Task<IReadOnlyList<SlaClock>> GetActiveByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default)
    {
        var activeStates = new[] { (int)ClockState.Running, (int)ClockState.AtRisk };

        var entities = await context.SlaClocks
            .Where(c => c.OrderId == orderId.ToString() && activeStates.Contains(c.State))
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SlaClock>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfUtc = asOf.UtcDateTime;

        var entities = await context.SlaClocks
            .Where(c => c.State == (int)ClockState.Running)
            .Where(c => c.AtRiskAt == null)
            .Where(c => c.AtRiskThresholdAt <= asOfUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SlaClock>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default)
    {
        var asOfUtc = asOf.UtcDateTime;
        var activeStates = new[] { (int)ClockState.Running, (int)ClockState.AtRisk };

        var entities = await context.SlaClocks
            .Where(c => activeStates.Contains(c.State))
            .Where(c => c.BreachedAt == null)
            .Where(c => c.DeadlineAt <= asOfUtc)
            .ToListAsync(cancellationToken);

        return entities.Select(ToDomain).ToList();
    }

    public void Add(SlaClock clock)
    {
        context.SlaClocks.Add(ToDb(clock));
    }

    public void Update(SlaClock clock)
    {
        var entity = ToDb(clock);
        context.SlaClocks.Update(entity);
    }

    private static SlaClock ToDomain(SlaClockDb entity) => SlaClock.Rehydrate(
        UlidId.Parse(entity.Id),
        UlidId.Parse(entity.OrderId),
        UlidId.Parse(entity.CustomerId),
        (ClockKind)entity.Kind,
        (ClockState)entity.State,
        new DateTimeOffset(entity.StartedAt, TimeSpan.Zero),
        new DateTimeOffset(entity.DeadlineAt, TimeSpan.Zero),
        new DateTimeOffset(entity.AtRiskThresholdAt, TimeSpan.Zero),
        entity.AtRiskAt.HasValue ? new DateTimeOffset(entity.AtRiskAt.Value, TimeSpan.Zero) : null,
        entity.BreachedAt.HasValue ? new DateTimeOffset(entity.BreachedAt.Value, TimeSpan.Zero) : null,
        entity.PausedAt.HasValue ? new DateTimeOffset(entity.PausedAt.Value, TimeSpan.Zero) : null,
        entity.CompletedAt.HasValue ? new DateTimeOffset(entity.CompletedAt.Value, TimeSpan.Zero) : null,
        entity.PauseReason,
        TimeSpan.FromMilliseconds(entity.AccumulatedPauseMs),
        entity.TargetBusinessDays,
        entity.AtRiskThresholdPercent);

    private static SlaClockDb ToDb(SlaClock clock) => new()
    {
        Id = clock.Id.ToString(),
        OrderId = clock.OrderId.ToString(),
        CustomerId = clock.CustomerId.ToString(),
        Kind = (int)clock.Kind,
        State = (int)clock.State,
        StartedAt = clock.StartedAt.UtcDateTime,
        DeadlineAt = clock.DeadlineAt.UtcDateTime,
        AtRiskThresholdAt = clock.AtRiskThresholdAt.UtcDateTime,
        AtRiskAt = clock.AtRiskAt?.UtcDateTime,
        BreachedAt = clock.BreachedAt?.UtcDateTime,
        PausedAt = clock.PausedAt?.UtcDateTime,
        CompletedAt = clock.CompletedAt?.UtcDateTime,
        PauseReason = clock.PauseReason,
        AccumulatedPauseMs = (long)clock.AccumulatedPauseTime.TotalMilliseconds,
        TargetBusinessDays = clock.TargetBusinessDays,
        AtRiskThresholdPercent = clock.AtRiskThresholdPercent
    };
}
