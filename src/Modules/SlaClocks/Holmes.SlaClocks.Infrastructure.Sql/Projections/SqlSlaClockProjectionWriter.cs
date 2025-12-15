using Holmes.SlaClocks.Application.Abstractions.Projections;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.SlaClocks.Infrastructure.Sql.Projections;

public sealed class SqlSlaClockProjectionWriter(
    SlaClockDbContext dbContext,
    ILogger<SqlSlaClockProjectionWriter> logger
) : ISlaClockProjectionWriter
{
    public async Task UpsertAsync(SlaClockProjectionModel model, CancellationToken cancellationToken)
    {
        var record = await dbContext.SlaClockProjections
            .FirstOrDefaultAsync(x => x.Id == model.ClockId, cancellationToken);

        if (record is null)
        {
            record = new SlaClockProjectionDb
            {
                Id = model.ClockId
            };
            dbContext.SlaClockProjections.Add(record);
        }

        record.OrderId = model.OrderId;
        record.CustomerId = model.CustomerId;
        record.Kind = (int)model.Kind;
        record.State = (int)model.State;
        record.StartedAt = model.StartedAt.UtcDateTime;
        record.DeadlineAt = model.DeadlineAt.UtcDateTime;
        record.AtRiskThresholdAt = model.AtRiskThresholdAt.UtcDateTime;
        record.TargetBusinessDays = model.TargetBusinessDays;
        record.AtRiskThresholdPercent = model.AtRiskThresholdPercent;
        record.AccumulatedPauseMs = 0;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateStateAsync(string clockId, ClockState state, CancellationToken cancellationToken)
    {
        var record = await dbContext.SlaClockProjections
            .FirstOrDefaultAsync(x => x.Id == clockId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("SLA clock projection not found for state update: {ClockId}", clockId);
            return;
        }

        record.State = (int)state;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdatePauseInfoAsync(
        string clockId,
        ClockState state,
        DateTimeOffset pausedAt,
        string pauseReason,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.SlaClockProjections
            .FirstOrDefaultAsync(x => x.Id == clockId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("SLA clock projection not found for pause update: {ClockId}", clockId);
            return;
        }

        record.State = (int)state;
        record.PausedAt = pausedAt.UtcDateTime;
        record.PauseReason = pauseReason;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateResumeInfoAsync(
        string clockId,
        ClockState state,
        DateTimeOffset deadlineAt,
        DateTimeOffset atRiskThresholdAt,
        TimeSpan accumulatedPauseTime,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.SlaClockProjections
            .FirstOrDefaultAsync(x => x.Id == clockId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("SLA clock projection not found for resume update: {ClockId}", clockId);
            return;
        }

        record.State = (int)state;
        record.PausedAt = null;
        record.PauseReason = null;
        record.DeadlineAt = deadlineAt.UtcDateTime;
        record.AtRiskThresholdAt = atRiskThresholdAt.UtcDateTime;
        record.AccumulatedPauseMs = (long)accumulatedPauseTime.TotalMilliseconds;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAtRiskAsync(
        string clockId,
        ClockState state,
        DateTimeOffset atRiskAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.SlaClockProjections
            .FirstOrDefaultAsync(x => x.Id == clockId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("SLA clock projection not found for at-risk update: {ClockId}", clockId);
            return;
        }

        record.State = (int)state;
        record.AtRiskAt = atRiskAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateBreachedAsync(
        string clockId,
        ClockState state,
        DateTimeOffset breachedAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.SlaClockProjections
            .FirstOrDefaultAsync(x => x.Id == clockId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("SLA clock projection not found for breach update: {ClockId}", clockId);
            return;
        }

        record.State = (int)state;
        record.BreachedAt = breachedAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCompletedAsync(
        string clockId,
        ClockState state,
        DateTimeOffset completedAt,
        CancellationToken cancellationToken
    )
    {
        var record = await dbContext.SlaClockProjections
            .FirstOrDefaultAsync(x => x.Id == clockId, cancellationToken);

        if (record is null)
        {
            logger.LogWarning("SLA clock projection not found for completion update: {ClockId}", clockId);
            return;
        }

        record.State = (int)state;
        record.CompletedAt = completedAt.UtcDateTime;

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}