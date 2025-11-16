using System.Text.Json;
using Holmes.App.Server.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController(
    WorkflowDbContext workflowDbContext,
    CustomersDbContext customersDbContext,
    UsersDbContext usersDbContext,
    ICurrentUserInitializer currentUserInitializer
) : ControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<PaginatedResponse<OrderSummaryResponse>>> GetSummaryAsync(
        [FromQuery] OrderSummaryQuery query,
        CancellationToken cancellationToken
    )
    {
        var actor = await EnsureCurrentUserAsync(cancellationToken);
        var access = await GetCustomerAccessAsync(actor, cancellationToken);

        var (page, pageSize) = NormalizePagination(query.Page, query.PageSize);
        if (!access.AllowsAll && (access.AllowedCustomerIds is null || access.AllowedCustomerIds.Count == 0))
        {
            return Ok(PaginatedResponse<OrderSummaryResponse>.Create(
                Array.Empty<OrderSummaryResponse>(),
                page,
                pageSize,
                0));
        }

        var summaries = workflowDbContext.OrderSummaries.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.OrderId))
        {
            if (!Ulid.TryParse(query.OrderId, out var parsedOrder))
            {
                return BadRequest("Invalid order id format.");
            }

            var orderId = parsedOrder.ToString();
            summaries = summaries.Where(o => o.OrderId == orderId);
        }

        if (!string.IsNullOrWhiteSpace(query.SubjectId))
        {
            if (!Ulid.TryParse(query.SubjectId, out var parsedSubject))
            {
                return BadRequest("Invalid subject id format.");
            }

            var subjectId = parsedSubject.ToString();
            summaries = summaries.Where(o => o.SubjectId == subjectId);
        }

        if (!string.IsNullOrWhiteSpace(query.CustomerId))
        {
            if (!Ulid.TryParse(query.CustomerId, out var parsedCustomer))
            {
                return BadRequest("Invalid customer id format.");
            }

            var customerId = parsedCustomer.ToString();
            if (!access.AllowsAll && !(access.AllowedCustomerIds?.Contains(customerId) ?? false))
            {
                return Forbid();
            }

            summaries = summaries.Where(o => o.CustomerId == customerId);
        }
        else if (!access.AllowsAll)
        {
            var allowed = access.AllowedCustomerIds ?? Array.Empty<string>();
            summaries = summaries.Where(o => allowed.Contains(o.CustomerId));
        }

        var statuses = NormalizeStatuses(query.Status);
        if (statuses.Count > 0)
        {
            summaries = summaries.Where(o => statuses.Contains(o.Status));
        }

        var totalItems = await summaries.CountAsync(cancellationToken);

        var items = await summaries
            .OrderByDescending(o => o.LastUpdatedAt)
            .ThenBy(o => o.OrderId)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new OrderSummaryResponse(
                o.OrderId,
                o.SubjectId,
                o.CustomerId,
                o.PolicySnapshotId,
                o.PackageCode,
                o.Status,
                o.LastStatusReason,
                o.LastUpdatedAt,
                o.ReadyForRoutingAt,
                o.ClosedAt,
                o.CanceledAt))
            .ToListAsync(cancellationToken);

        return Ok(PaginatedResponse<OrderSummaryResponse>.Create(items, page, pageSize, totalItems));
    }

    [HttpGet("{orderId}/timeline")]
    public async Task<ActionResult<IReadOnlyCollection<OrderTimelineEntryResponse>>> GetTimelineAsync(
        string orderId,
        [FromQuery] OrderTimelineQuery query,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(orderId, out var parsedOrder))
        {
            return BadRequest("Invalid order id format.");
        }

        var actor = await EnsureCurrentUserAsync(cancellationToken);
        var access = await GetCustomerAccessAsync(actor, cancellationToken);
        var targetOrderId = parsedOrder.ToString();

        var orderSummary = await workflowDbContext.OrderSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == targetOrderId, cancellationToken);

        if (orderSummary is null)
        {
            return NotFound();
        }

        if (!access.AllowsAll && !(access.AllowedCustomerIds?.Contains(orderSummary.CustomerId) ?? false))
        {
            return Forbid();
        }

        var limit = Math.Clamp(query.Limit, 1, 200);
        var timeline = workflowDbContext.OrderTimelineEvents
            .AsNoTracking()
            .Where(e => e.OrderId == targetOrderId);

        if (query.Before.HasValue)
        {
            timeline = timeline.Where(e => e.OccurredAt < query.Before.Value);
        }

        var events = await timeline
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.EventId)
            .Take(limit)
            .Select(e => new OrderTimelineEntryResponse(
                e.EventId,
                e.OrderId,
                e.EventType,
                e.Description,
                e.Source,
                e.OccurredAt,
                e.RecordedAt,
                DeserializeMetadata(e.MetadataJson)))
            .ToListAsync(cancellationToken);

        return Ok(events);
    }

    private async Task<UlidId> EnsureCurrentUserAsync(CancellationToken cancellationToken)
    {
        return await currentUserInitializer.EnsureCurrentUserIdAsync(cancellationToken);
    }

    private async Task<CustomerAccess> GetCustomerAccessAsync(UlidId userId, CancellationToken cancellationToken)
    {
        if (await IsGlobalAdminAsync(userId, cancellationToken))
        {
            return CustomerAccess.All;
        }

        var customerIds = await customersDbContext.CustomerAdmins
            .AsNoTracking()
            .Where(a => a.UserId == userId.ToString())
            .Select(a => a.CustomerId)
            .Distinct()
            .ToListAsync(cancellationToken);

        return customerIds.Count == 0
            ? CustomerAccess.None
            : new CustomerAccess(false, customerIds);
    }

    private Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        return usersDbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(x =>
                    x.UserId == userId.ToString() &&
                    x.Role == UserRole.Admin &&
                    x.CustomerId == null,
                cancellationToken);
    }

    private static (int Page, int PageSize) NormalizePagination(int page, int pageSize)
    {
        var currentPage = page <= 0 ? 1 : page;
        var size = pageSize <= 0 ? 25 : Math.Min(pageSize, 100);
        return (currentPage, size);
    }

    private static IReadOnlyCollection<string> NormalizeStatuses(IReadOnlyCollection<string>? statuses)
    {
        if (statuses is null || statuses.Count == 0)
        {
            return Array.Empty<string>();
        }

        var normalized = new List<string>(statuses.Count);
        foreach (var candidate in statuses)
        {
            if (Enum.TryParse<OrderStatus>(candidate, true, out var parsed))
            {
                normalized.Add(parsed.ToString());
            }
        }

        return normalized;
    }

    private static JsonElement? DeserializeMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        return JsonSerializer.Deserialize<JsonElement>(metadataJson);
    }

    private sealed record CustomerAccess(bool AllowsAll, IReadOnlyList<string>? AllowedCustomerIds)
    {
        public static CustomerAccess All { get; } = new(true, null);

        public static CustomerAccess None { get; } = new(false, Array.Empty<string>());
    }
}

public sealed record OrderSummaryResponse(
    string OrderId,
    string SubjectId,
    string CustomerId,
    string PolicySnapshotId,
    string? PackageCode,
    string Status,
    string? LastStatusReason,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? ReadyForRoutingAt,
    DateTimeOffset? ClosedAt,
    DateTimeOffset? CanceledAt
);

public sealed record OrderTimelineEntryResponse(
    string EventId,
    string OrderId,
    string EventType,
    string Description,
    string Source,
    DateTimeOffset OccurredAt,
    DateTimeOffset RecordedAt,
    JsonElement? Metadata
);

public sealed record OrderSummaryQuery
{
    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;

    public string? CustomerId { get; init; }

    public string? SubjectId { get; init; }

    public string? OrderId { get; init; }

    public IReadOnlyCollection<string>? Status { get; init; }
}

public sealed record OrderTimelineQuery
{
    public DateTimeOffset? Before { get; init; }

    public int Limit { get; init; } = 50;
}