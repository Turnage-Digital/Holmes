using System.Text.Json;
using Holmes.Core.Infrastructure.Sql;
using Holmes.Core.Infrastructure.Sql.Entities;
using Holmes.Core.Infrastructure.Sql.Projections;
using Holmes.Workflow.Application.Abstractions.Projections;
using Holmes.Workflow.Infrastructure.Sql.Entities;
using Holmes.Workflow.Infrastructure.Sql.Mappers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Holmes.Workflow.Infrastructure.Sql.Projections;

public sealed class OrderSummaryProjectionRunner(
    WorkflowDbContext workflowDbContext,
    CoreDbContext coreDbContext,
    IOrderSummaryWriter orderSummaryWriter,
    ILogger<OrderSummaryProjectionRunner> logger
)
{
    private const string ProjectionName = "workflow.order_summary";
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

            foreach (var entity in batch)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var order = OrderEntityMapper.Rehydrate(entity);
                await orderSummaryWriter.UpsertAsync(order, cancellationToken);
                processed++;
                position++;
                lastCursor = new CursorState(entity.LastUpdatedAt, entity.OrderId);
            }

            if (lastCursor is not null)
            {
                await SaveCheckpointAsync(position, lastCursor, cancellationToken);
            }
        }

        logger.LogInformation(
            "Order summary replay processed {Count} orders. Last cursor: {Cursor}",
            processed,
            lastCursor is null
                ? "none"
                : $"{lastCursor.OrderId}/{lastCursor.LastUpdatedAt:O}");

        return new ProjectionReplayResult(
            processed,
            lastCursor?.LastUpdatedAt,
            lastCursor?.OrderId);
    }

    private async Task<(long position, CursorState? cursor)> LoadCheckpointAsync(CancellationToken cancellationToken)
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
            var cursor = JsonSerializer.Deserialize<CursorState>(checkpoint.Cursor, SerializerOptions);
            return (checkpoint.Position, cursor);
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Invalid cursor for {ProjectionName}; replaying from start.", ProjectionName);
            return (0, null);
        }
    }

    private async Task<List<OrderDb>> LoadBatchAsync(CursorState? cursor, CancellationToken cancellationToken)
    {
        var query = workflowDbContext.Orders.AsNoTracking();

        if (cursor is not null)
        {
            query = query.Where(o =>
                o.LastUpdatedAt > cursor.LastUpdatedAt ||
                (o.LastUpdatedAt == cursor.LastUpdatedAt &&
                 string.CompareOrdinal(o.OrderId, cursor.OrderId) > 0));
        }

        return await query
            .OrderBy(o => o.LastUpdatedAt)
            .ThenBy(o => o.OrderId)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);
    }

    private async Task SaveCheckpointAsync(
        long position,
        CursorState cursor,
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
        if (workflowDbContext.Database.IsRelational())
        {
            await workflowDbContext.OrderSummaries.ExecuteDeleteAsync(cancellationToken);
        }
        else
        {
            workflowDbContext.OrderSummaries.RemoveRange(workflowDbContext.OrderSummaries);
            await workflowDbContext.SaveChangesAsync(cancellationToken);
        }

        workflowDbContext.ChangeTracker.Clear();

        var checkpoint = await coreDbContext.ProjectionCheckpoints
            .FirstOrDefaultAsync(x => x.ProjectionName == ProjectionName && x.TenantId == "*", cancellationToken);

        if (checkpoint is not null)
        {
            coreDbContext.ProjectionCheckpoints.Remove(checkpoint);
            await coreDbContext.SaveChangesAsync(cancellationToken);
        }
    }

    private sealed record CursorState(DateTimeOffset LastUpdatedAt, string OrderId);
}