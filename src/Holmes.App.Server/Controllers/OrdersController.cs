using System.Text.Json;
using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Subjects.Infrastructure.Sql;
using Holmes.Workflow.Application.Abstractions.Dtos;
using Holmes.Workflow.Application.Commands;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController(
    IMediator mediator,
    WorkflowDbContext workflowDbContext,
    CustomersDbContext customersDbContext,
    SubjectsDbContext subjectsDbContext,
    ICurrentUserAccess currentUserAccess
) : ControllerBase
{
    [HttpPost]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<ActionResult<OrderSummaryDto>> CreateOrderAsync(
        [FromBody] CreateOrderRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(request.CustomerId, out var parsedCustomer))
        {
            return BadRequest("Invalid customer id format.");
        }

        if (!Ulid.TryParse(request.SubjectId, out var parsedSubject))
        {
            return BadRequest("Invalid subject id format.");
        }

        var customerId = parsedCustomer.ToString();
        if (!await currentUserAccess.HasCustomerAccessAsync(customerId, cancellationToken))
        {
            return Forbid();
        }

        var customerExists = await customersDbContext.Customers
            .AsNoTracking()
            .AnyAsync(c => c.CustomerId == customerId, cancellationToken);

        if (!customerExists)
        {
            return NotFound($"Customer '{customerId}' not found.");
        }

        var subjectId = parsedSubject.ToString();
        var subjectExists = await subjectsDbContext.Subjects
            .AsNoTracking()
            .AnyAsync(s => s.SubjectId == subjectId, cancellationToken);

        if (!subjectExists)
        {
            return NotFound($"Subject '{subjectId}' not found.");
        }

        var orderId = UlidId.NewUlid();
        var timestamp = DateTimeOffset.UtcNow;
        var command = new CreateOrderCommand(
            orderId,
            UlidId.FromUlid(parsedSubject),
            UlidId.FromUlid(parsedCustomer),
            request.PolicySnapshotId,
            timestamp,
            request.PackageCode);
        
        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var summary = await workflowDbContext.OrderSummaries
            .AsNoTracking()
            .Where(o => o.OrderId == orderId.ToString())
            .Select(o => new OrderSummaryDto(
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
            .SingleOrDefaultAsync(cancellationToken);

        if (summary is null)
        {
            return Problem("Failed to load created order.");
        }

        return Created($"/api/orders/{orderId}/timeline", summary);
    }

    [HttpGet("summary")]
    public async Task<ActionResult<PaginatedResponse<OrderSummaryDto>>> GetSummaryAsync(
        [FromQuery] OrderSummaryQuery query,
        CancellationToken cancellationToken
    )
    {
        var isGlobalAdmin = await currentUserAccess.IsGlobalAdminAsync(cancellationToken);
        var allowedCustomers = isGlobalAdmin
            ? []
            : await currentUserAccess.GetAllowedCustomerIdsAsync(cancellationToken);

        var (page, pageSize) = PaginationNormalization.Normalize(query.Page, query.PageSize);
        if (!isGlobalAdmin && allowedCustomers.Count == 0)
        {
            return Ok(PaginatedResponse<OrderSummaryDto>.Create(
                [],
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
            if (!isGlobalAdmin && !allowedCustomers.Contains(customerId))
            {
                return Forbid();
            }

            summaries = summaries.Where(o => o.CustomerId == customerId);
        }
        else if (!isGlobalAdmin)
        {
            summaries = summaries.Where(o => allowedCustomers.Contains(o.CustomerId));
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
            .Select(o => new OrderSummaryDto(
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

        return Ok(PaginatedResponse<OrderSummaryDto>.Create(items, page, pageSize, totalItems));
    }

    [HttpGet("stats")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<ActionResult<OrderStatsDto>> GetStatsAsync(CancellationToken cancellationToken)
    {
        var isGlobalAdmin = await currentUserAccess.IsGlobalAdminAsync(cancellationToken);
        var allowedCustomers = isGlobalAdmin
            ? []
            : await currentUserAccess.GetAllowedCustomerIdsAsync(cancellationToken);

        var summaries = workflowDbContext.OrderSummaries.AsNoTracking().AsQueryable();
        if (!isGlobalAdmin)
        {
            if (allowedCustomers.Count == 0)
            {
                return Ok(new OrderStatsDto(0, 0, 0, 0, 0, 0));
            }

            summaries = summaries.Where(o => allowedCustomers.Contains(o.CustomerId));
        }

        var grouped = await summaries
            .GroupBy(o => o.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var lookup = grouped.ToDictionary(x => x.Status, x => x.Count, StringComparer.Ordinal);

        int GetCount(OrderStatus status)
        {
            return lookup.TryGetValue(status.ToString(), out var value) ? value : 0;
        }

        var stats = new OrderStatsDto(
            GetCount(OrderStatus.Invited),
            GetCount(OrderStatus.IntakeInProgress),
            GetCount(OrderStatus.IntakeComplete),
            GetCount(OrderStatus.ReadyForRouting),
            GetCount(OrderStatus.Blocked),
            GetCount(OrderStatus.Canceled));

        return Ok(stats);
    }

    [HttpGet("{orderId}/timeline")]
    public async Task<ActionResult<IReadOnlyCollection<OrderTimelineEntryDto>>> GetTimelineAsync(
        string orderId,
        [FromQuery] OrderTimelineQuery query,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(orderId, out var parsedOrder))
        {
            return BadRequest("Invalid order id format.");
        }

        var targetOrderId = parsedOrder.ToString();

        var orderSummary = await workflowDbContext.OrderSummaries
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.OrderId == targetOrderId, cancellationToken);

        if (orderSummary is null)
        {
            return NotFound();
        }

        if (!await currentUserAccess.HasCustomerAccessAsync(orderSummary.CustomerId, cancellationToken))
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

        return Ok(events);
    }

    private static IReadOnlyCollection<string> NormalizeStatuses(IReadOnlyCollection<string>? statuses)
    {
        if (statuses is null || statuses.Count is 0)
        {
            return [];
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

    public sealed record CreateOrderRequest(
        string CustomerId,
        string SubjectId,
        string PolicySnapshotId,
        string? PackageCode = null
    );
}