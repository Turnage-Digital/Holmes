using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Entities;

namespace Holmes.SlaClocks.Infrastructure.Sql.Mappers;

public static class SlaClockMapper
{
    public static SlaClock ToDomain(SlaClockDb entity)
    {
        return SlaClock.Rehydrate(
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
    }

    public static SlaClockDb ToDb(SlaClock clock)
    {
        return new SlaClockDb
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

    public static void UpdateDb(SlaClockDb db, SlaClock clock)
    {
        db.State = (int)clock.State;
        db.AtRiskAt = clock.AtRiskAt?.UtcDateTime;
        db.BreachedAt = clock.BreachedAt?.UtcDateTime;
        db.PausedAt = clock.PausedAt?.UtcDateTime;
        db.CompletedAt = clock.CompletedAt?.UtcDateTime;
        db.PauseReason = clock.PauseReason;
        db.AccumulatedPauseMs = (long)clock.AccumulatedPauseTime.TotalMilliseconds;
    }
}