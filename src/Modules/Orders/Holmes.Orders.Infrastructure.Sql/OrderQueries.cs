using System.Text.Json;
using Holmes.Orders.Application.Abstractions;
using Holmes.Orders.Application.Abstractions.Dtos;
using Holmes.Orders.Domain;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Orders.Infrastructure.Sql;

public sealed class OrderQueries(OrdersDbContext dbContext) : IOrderQueries
{
    public async Task<OrderSummaryDto?> GetSummaryByIdAsync(string orderId, CancellationToken cancellationToken)
    {
        return await dbContext.OrderSummaries
            .AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => new OrderSummaryDto(
                o.OrderId,
                o.SubjectId,
                o.CustomerId,
                o.PolicySnapshotId,
                o.PackageCode,
                o.Status,
                o.LastStatusReason,
                o.LastUpdatedAt,
                o.ReadyForFulfillmentAt,
                o.ClosedAt,
                o.CanceledAt))
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<OrderSummaryPagedResult> GetSummariesPagedAsync(
        OrderSummaryFilter filter,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.OrderSummaries.AsNoTracking().AsQueryable();

        // Apply filters
        if (filter.OrderId is not null)
        {
            query = query.Where(o => o.OrderId == filter.OrderId);
        }

        if (filter.SubjectId is not null)
        {
            query = query.Where(o => o.SubjectId == filter.SubjectId);
        }

        if (filter.CustomerId is not null)
        {
            query = query.Where(o => o.CustomerId == filter.CustomerId);
        }
        else if (filter.AllowedCustomerIds is not null && filter.AllowedCustomerIds.Count > 0)
        {
            query = query.Where(o => filter.AllowedCustomerIds.Contains(o.CustomerId));
        }

        if (filter.Statuses is not null && filter.Statuses.Count > 0)
        {
            query = query.Where(o => filter.Statuses.Contains(o.Status));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(o => o.LastUpdatedAt)
            .ThenBy(o => o.OrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryDto(
                o.OrderId,
                o.SubjectId,
                o.CustomerId,
                o.PolicySnapshotId,
                o.PackageCode,
                o.Status,
                o.LastStatusReason,
                o.LastUpdatedAt,
                o.ReadyForFulfillmentAt,
                o.ClosedAt,
                o.CanceledAt))
            .ToListAsync(cancellationToken);

        return new OrderSummaryPagedResult(items, totalCount);
    }

    public async Task<OrderStatsDto> GetStatsAsync(
        IReadOnlyCollection<string>? allowedCustomerIds,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.OrderSummaries.AsNoTracking().AsQueryable();

        if (allowedCustomerIds is not null && allowedCustomerIds.Count > 0)
        {
            query = query.Where(o => allowedCustomerIds.Contains(o.CustomerId));
        }

        var grouped = await query
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var lookup = grouped.ToDictionary(x => x.Status, x => x.Count, StringComparer.Ordinal);

        int GetCount(OrderStatus status)
        {
            return lookup.TryGetValue(status.ToString(), out var value) ? value : 0;
        }

        return new OrderStatsDto(
            GetCount(OrderStatus.Invited),
            GetCount(OrderStatus.IntakeInProgress),
            GetCount(OrderStatus.IntakeComplete),
            GetCount(OrderStatus.ReadyForFulfillment),
            GetCount(OrderStatus.Blocked),
            GetCount(OrderStatus.Canceled));
    }

    public async Task<IReadOnlyList<OrderTimelineEntryDto>> GetTimelineAsync(
        string orderId,
        DateTimeOffset? before,
        int limit,
        CancellationToken cancellationToken
    )
    {
        var query = dbContext.OrderTimelineEvents
            .AsNoTracking()
            .Where(e => e.OrderId == orderId);

        if (before.HasValue)
        {
            query = query.Where(e => e.OccurredAt < before.Value);
        }

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.EventId)
            .Take(limit)
            .Select(e => new OrderTimelineEntryDto(
                e.EventId,
                e.OrderId,
                e.EventType,
                e.Description,
                e.Source,
                e.OccurredAt,
                e.RecordedAt,
                DeserializeMetadata(e.MetadataJson)))
            .ToListAsync(cancellationToken);
    }

    public async Task<string?> GetCustomerIdAsync(string orderId, CancellationToken cancellationToken)
    {
        return await dbContext.OrderSummaries
            .AsNoTracking()
            .Where(o => o.OrderId == orderId)
            .Select(o => o.CustomerId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static JsonElement? DeserializeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<JsonElement>(metadataJson);
    }
}