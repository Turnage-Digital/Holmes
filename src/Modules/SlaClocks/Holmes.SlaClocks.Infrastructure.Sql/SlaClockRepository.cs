using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Mappers;
using Holmes.SlaClocks.Infrastructure.Sql.Specifications;
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
        var spec = new SlaClockByOrderIdSpec(orderId.ToString());

        var entities = await context.SlaClocks
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return entities.Select(SlaClockMapper.ToDomain).ToList();
    }

    public async Task<SlaClock?> GetByOrderIdAndKindAsync(
        UlidId orderId,
        ClockKind kind,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new SlaClockByOrderIdAndKindSpec(orderId.ToString(), kind);

        var entity = await context.SlaClocks
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        return entity is null ? null : SlaClockMapper.ToDomain(entity);
    }

    public async Task<IReadOnlyList<SlaClock>> GetActiveByOrderIdAsync(
        UlidId orderId,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new ActiveSlaClocksByOrderIdSpec(orderId.ToString());

        var entities = await context.SlaClocks
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return entities.Select(SlaClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SlaClock>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new RunningClocksPastThresholdSpec(asOf.UtcDateTime);

        var entities = await context.SlaClocks
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return entities.Select(SlaClockMapper.ToDomain).ToList();
    }

    public async Task<IReadOnlyList<SlaClock>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken = default
    )
    {
        var spec = new RunningClocksPastDeadlineSpec(asOf.UtcDateTime);

        var entities = await context.SlaClocks
            .ApplySpecification(spec)
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