using System.Text.Json;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Entities;
using Holmes.Core.Infrastructure.Sql.Projections;
using Holmes.Intake.Application.Projections;
using Holmes.Intake.Domain;
using Holmes.Intake.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Intake.Infrastructure.Sql.Projections;

public sealed class IntakeSessionProjectionRunner(
    IntakeDbContext intakeDbContext,
    CoreDbContext coreDbContext,
    IIntakeSessionProjectionWriter projectionWriter,
    ILogger<IntakeSessionProjectionRunner> logger
)
{
    private const string ProjectionName = "intake.sessions";
    private const int BatchSize = 200;
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task<ProjectionReplayResult> RunAsync(bool reset, CancellationToken cancellationToken)
    {
        if (reset)
        {
            await ResetStateAsync(cancellationToken);
        }

        var (position, cursor) = await LoadCheckpointAsync(cancellationToken);
        var processed = 0;
        var lastCursor = cursor;

        while (true)
        {
            var batch = await LoadBatchAsync(lastCursor, cancellationToken);
            if (batch.Count == 0)
            {
                break;
            }

            foreach (var record in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var model = ToProjectionModel(record);
                var updated = await projectionWriter.UpdateAsync(model.IntakeSessionId, _ => model, cancellationToken);
                if (!updated)
                {
                    await projectionWriter.CreateAsync(model, cancellationToken);
                }

                processed++;
                position++;
                lastCursor = new IntakeCursor(record.LastTouchedAt, record.IntakeSessionId);
            }

            if (lastCursor is not null)
            {
                await SaveCheckpointAsync(position, lastCursor, cancellationToken);
            }
        }

        logger.LogInformation(
            "Intake session replay processed {Count} sessions. Last cursor: {Cursor}",
            processed,
            lastCursor is null
                ? "none"
                : $"{lastCursor.IntakeSessionId}/{lastCursor.LastTouchedAt:O}");

        return new ProjectionReplayResult(
            processed,
            lastCursor?.LastTouchedAt,
            lastCursor?.IntakeSessionId);
    }

    private async Task<(long position, IntakeCursor? cursor)> LoadCheckpointAsync(CancellationToken cancellationToken)
    {
        var checkpoint = await coreDbContext.ProjectionCheckpoints
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName && x.TenantId == "*", cancellationToken);

        if (checkpoint is null || string.IsNullOrWhiteSpace(checkpoint.Cursor))
        {
            return (checkpoint?.Position ?? 0, null);
        }

        try
        {
            var cursor = JsonSerializer.Deserialize<IntakeCursor>(checkpoint.Cursor, SerializerOptions);
            return (checkpoint.Position, cursor);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid cursor for {ProjectionName}; replaying from start.", ProjectionName);
            return (0, null);
        }
    }

    private async Task<List<IntakeSessionDb>> LoadBatchAsync(
        IntakeCursor? cursor,
        CancellationToken cancellationToken
    )
    {
        IQueryable<IntakeSessionDb> query = intakeDbContext.IntakeSessions.AsNoTracking();

        if (cursor is not null)
        {
            query = query.Where(s =>
                s.LastTouchedAt > cursor.LastTouchedAt ||
                (s.LastTouchedAt == cursor.LastTouchedAt &&
                 string.CompareOrdinal(s.IntakeSessionId, cursor.IntakeSessionId) > 0));
        }

        return await query
            .OrderBy(s => s.LastTouchedAt)
            .ThenBy(s => s.IntakeSessionId)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
    }

    private async Task SaveCheckpointAsync(
        long position,
        IntakeCursor cursor,
        CancellationToken cancellationToken
    )
    {
        var checkpoint = await coreDbContext.ProjectionCheckpoints
            .FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName && x.TenantId == "*", cancellationToken);

        if (checkpoint is null)
        {
            checkpoint = new ProjectionCheckpoint
            {
                ProjectionName = ProjectionName,
                TenantId = "*"
            };
            coreDbContext.ProjectionCheckpoints.Add(checkpoint);
        }

        checkpoint.Position = position;
        checkpoint.Cursor = JsonSerializer.Serialize(cursor, SerializerOptions);
        checkpoint.UpdatedAt = DateTime.UtcNow;
        await coreDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task ResetStateAsync(CancellationToken cancellationToken)
    {
        if (intakeDbContext.Database.IsRelational())
        {
            await intakeDbContext.IntakeSessionProjections.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            intakeDbContext.IntakeSessionProjections.RemoveRange(intakeDbContext.IntakeSessionProjections);
            await intakeDbContext.SaveChangesAsync(cancellationToken);
        }

        intakeDbContext.ChangeTracker.Clear();

        var checkpoint = await coreDbContext.ProjectionCheckpoints
            .FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName && x.TenantId == "*", cancellationToken);

        if (checkpoint is not null)
        {
            coreDbContext.ProjectionCheckpoints.Remove(checkpoint);
            await coreDbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private static IntakeSessionProjectionModel ToProjectionModel(IntakeSessionDb record)
    {
        var policy = JsonSerializer.Deserialize<PolicySnapshotRecord>(record.PolicySnapshotJson)
                     ?? throw new InvalidOperationException("Policy snapshot payload invalid.");

        UlidId? superseded = string.IsNullOrWhiteSpace(record.SupersededBySessionId)
            ? null
            : UlidId.Parse(record.SupersededBySessionId);

        return new IntakeSessionProjectionModel(
            UlidId.Parse(record.IntakeSessionId),
            UlidId.Parse(record.OrderId),
            UlidId.Parse(record.SubjectId),
            UlidId.Parse(record.CustomerId),
            policy.SnapshotId,
            policy.SchemaVersion,
            Enum.Parse<IntakeSessionStatus>(record.Status),
            record.CreatedAt,
            record.LastTouchedAt,
            record.ExpiresAt,
            record.SubmittedAt,
            record.AcceptedAt,
            record.CancellationReason,
            superseded);
    }

    private sealed record IntakeCursor(DateTimeOffset LastTouchedAt, string IntakeSessionId);

    private sealed record PolicySnapshotRecord(
        string SnapshotId,
        string SchemaVersion,
        DateTimeOffset CapturedAt,
        IReadOnlyDictionary<string, string> Metadata
    );
}
