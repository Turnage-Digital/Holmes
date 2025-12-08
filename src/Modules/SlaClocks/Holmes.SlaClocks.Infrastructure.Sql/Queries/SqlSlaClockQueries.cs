using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.SlaClocks.Application.Abstractions.Dtos;
using Holmes.SlaClocks.Application.Abstractions.Queries;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.SlaClocks.Infrastructure.Sql.Queries;

public sealed class SqlSlaClockQueries(SlaClockDbContext dbContext) : ISlaClockQueries
{
    public async Task<IReadOnlyList<SlaClockDto>> GetByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    )
    {
        var spec = new SlaClockByOrderIdSpec(orderId);

        var entities = await dbContext.SlaClocks
            .AsNoTracking()
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return entities.Select(c => new SlaClockDto(
                UlidId.Parse(c.Id),
                UlidId.Parse(c.OrderId),
                UlidId.Parse(c.CustomerId),
                (ClockKind)c.Kind,
                (ClockState)c.State,
                new DateTimeOffset(c.StartedAt, TimeSpan.Zero),
                new DateTimeOffset(c.DeadlineAt, TimeSpan.Zero),
                new DateTimeOffset(c.AtRiskThresholdAt, TimeSpan.Zero),
                c.AtRiskAt.HasValue ? new DateTimeOffset(c.AtRiskAt.Value, TimeSpan.Zero) : null,
                c.BreachedAt.HasValue ? new DateTimeOffset(c.BreachedAt.Value, TimeSpan.Zero) : null,
                c.PausedAt.HasValue ? new DateTimeOffset(c.PausedAt.Value, TimeSpan.Zero) : null,
                c.CompletedAt.HasValue ? new DateTimeOffset(c.CompletedAt.Value, TimeSpan.Zero) : null,
                c.PauseReason,
                TimeSpan.FromMilliseconds(c.AccumulatedPauseMs),
                c.TargetBusinessDays,
                c.AtRiskThresholdPercent
            ))
            .ToList();
    }

    public async Task<SlaClockDto?> GetByOrderIdAndKindAsync(
        string orderId,
        ClockKind kind,
        CancellationToken cancellationToken
    )
    {
        var spec = new SlaClockByOrderIdAndKindSpec(orderId, kind);

        var entity = await dbContext.SlaClocks
            .AsNoTracking()
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (entity is null)
        {
            return null;
        }

        return new SlaClockDto(
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
            entity.AtRiskThresholdPercent
        );
    }

    public async Task<IReadOnlyList<SlaClockDto>> GetActiveByOrderIdAsync(
        string orderId,
        CancellationToken cancellationToken
    )
    {
        var spec = new ActiveSlaClocksByOrderIdSpec(orderId);

        var entities = await dbContext.SlaClocks
            .AsNoTracking()
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        return entities.Select(c => new SlaClockDto(
                UlidId.Parse(c.Id),
                UlidId.Parse(c.OrderId),
                UlidId.Parse(c.CustomerId),
                (ClockKind)c.Kind,
                (ClockState)c.State,
                new DateTimeOffset(c.StartedAt, TimeSpan.Zero),
                new DateTimeOffset(c.DeadlineAt, TimeSpan.Zero),
                new DateTimeOffset(c.AtRiskThresholdAt, TimeSpan.Zero),
                c.AtRiskAt.HasValue ? new DateTimeOffset(c.AtRiskAt.Value, TimeSpan.Zero) : null,
                c.BreachedAt.HasValue ? new DateTimeOffset(c.BreachedAt.Value, TimeSpan.Zero) : null,
                c.PausedAt.HasValue ? new DateTimeOffset(c.PausedAt.Value, TimeSpan.Zero) : null,
                c.CompletedAt.HasValue ? new DateTimeOffset(c.CompletedAt.Value, TimeSpan.Zero) : null,
                c.PauseReason,
                TimeSpan.FromMilliseconds(c.AccumulatedPauseMs),
                c.TargetBusinessDays,
                c.AtRiskThresholdPercent
            ))
            .ToList();
    }

    public async Task<IReadOnlyList<SlaClockWatchdogDto>> GetRunningClocksPastThresholdAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var spec = new RunningClocksPastThresholdSpec(asOf.UtcDateTime);

        return await dbContext.SlaClocks
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(c => new SlaClockWatchdogDto(
                c.Id,
                c.OrderId,
                c.CustomerId,
                (ClockKind)c.Kind,
                (ClockState)c.State,
                new DateTimeOffset(c.StartedAt, TimeSpan.Zero),
                new DateTimeOffset(c.DeadlineAt, TimeSpan.Zero),
                new DateTimeOffset(c.AtRiskThresholdAt, TimeSpan.Zero)
            ))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<SlaClockWatchdogDto>> GetRunningClocksPastDeadlineAsync(
        DateTimeOffset asOf,
        CancellationToken cancellationToken
    )
    {
        var spec = new RunningClocksPastDeadlineSpec(asOf.UtcDateTime);

        return await dbContext.SlaClocks
            .AsNoTracking()
            .ApplySpecification(spec)
            .Select(c => new SlaClockWatchdogDto(
                c.Id,
                c.OrderId,
                c.CustomerId,
                (ClockKind)c.Kind,
                (ClockState)c.State,
                new DateTimeOffset(c.StartedAt, TimeSpan.Zero),
                new DateTimeOffset(c.DeadlineAt, TimeSpan.Zero),
                new DateTimeOffset(c.AtRiskThresholdAt, TimeSpan.Zero)
            ))
            .ToListAsync(cancellationToken);
    }
}