using System.Text.Json;
using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Abstractions.Queries;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Domain;
using Holmes.Subjects.Application.Abstractions.Queries;
using Holmes.Workflow.Application.Abstractions.Dtos;
using Holmes.Workflow.Application.Abstractions.Queries;
using Holmes.Workflow.Application.Commands;
using Holmes.Workflow.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController(
    IMediator mediator,
    IOrderQueries orderQueries,
    ICustomerQueries customerQueries,
    ISubjectQueries subjectQueries,
    IServiceRequestQueries serviceRequestQueries,
    ICurrentUserAccess currentUserAccess,
    IEventStore eventStore
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

        if (!await customerQueries.ExistsAsync(customerId, cancellationToken))
        {
            return NotFound($"Customer '{customerId}' not found.");
        }

        var subjectId = parsedSubject.ToString();
        if (!await subjectQueries.ExistsAsync(subjectId, cancellationToken))
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

        var summary = await orderQueries.GetSummaryByIdAsync(orderId.ToString(), cancellationToken);

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
            ? null
            : await currentUserAccess.GetAllowedCustomerIdsAsync(cancellationToken);

        var (page, pageSize) = PaginationNormalization.Normalize(query.Page, query.PageSize);
        if (!isGlobalAdmin && (allowedCustomers is null || allowedCustomers.Count == 0))
        {
            return Ok(PaginatedResponse<OrderSummaryDto>.Create([], page, pageSize, 0));
        }

        // Validate IDs
        string? orderId = null;
        if (!string.IsNullOrWhiteSpace(query.OrderId))
        {
            if (!Ulid.TryParse(query.OrderId, out var parsedOrder))
            {
                return BadRequest("Invalid order id format.");
            }

            orderId = parsedOrder.ToString();
        }

        string? subjectId = null;
        if (!string.IsNullOrWhiteSpace(query.SubjectId))
        {
            if (!Ulid.TryParse(query.SubjectId, out var parsedSubject))
            {
                return BadRequest("Invalid subject id format.");
            }

            subjectId = parsedSubject.ToString();
        }

        string? customerId = null;
        if (!string.IsNullOrWhiteSpace(query.CustomerId))
        {
            if (!Ulid.TryParse(query.CustomerId, out var parsedCustomer))
            {
                return BadRequest("Invalid customer id format.");
            }

            customerId = parsedCustomer.ToString();
            if (!isGlobalAdmin && allowedCustomers is not null && !allowedCustomers.Contains(customerId))
            {
                return Forbid();
            }
        }

        var filter = new OrderSummaryFilter(
            customerId is null ? allowedCustomers : null,
            orderId,
            subjectId,
            customerId,
            NormalizeStatuses(query.Status));

        var result = await orderQueries.GetSummariesPagedAsync(filter, page, pageSize, cancellationToken);

        return Ok(PaginatedResponse<OrderSummaryDto>.Create(
            result.Items.ToList(), page, pageSize, result.TotalCount));
    }

    [HttpGet("stats")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<ActionResult<OrderStatsDto>> GetStatsAsync(CancellationToken cancellationToken)
    {
        var isGlobalAdmin = await currentUserAccess.IsGlobalAdminAsync(cancellationToken);
        IReadOnlyCollection<string>? allowedCustomers = null;

        if (!isGlobalAdmin)
        {
            allowedCustomers = await currentUserAccess.GetAllowedCustomerIdsAsync(cancellationToken);
            if (allowedCustomers.Count == 0)
            {
                return Ok(new OrderStatsDto(0, 0, 0, 0, 0, 0));
            }
        }

        var stats = await orderQueries.GetStatsAsync(allowedCustomers, cancellationToken);
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

        var customerIdForOrder = await orderQueries.GetCustomerIdAsync(targetOrderId, cancellationToken);

        if (customerIdForOrder is null)
        {
            return NotFound();
        }

        if (!await currentUserAccess.HasCustomerAccessAsync(customerIdForOrder, cancellationToken))
        {
            return Forbid();
        }

        var limit = Math.Clamp(query.Limit, 1, 200);
        var events = await orderQueries.GetTimelineAsync(
            targetOrderId, query.Before, limit, cancellationToken);

        return Ok(events);
    }

    [HttpGet("{orderId}/services")]
    public async Task<ActionResult<OrderServicesDto>> GetServicesAsync(
        string orderId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(orderId, out var parsedOrder))
        {
            return BadRequest("Invalid order id format.");
        }

        var targetOrderId = parsedOrder.ToString();

        // Verify order exists and user has access
        var customerIdForOrder = await orderQueries.GetCustomerIdAsync(targetOrderId, cancellationToken);

        if (customerIdForOrder is null)
        {
            return NotFound();
        }

        if (!await currentUserAccess.HasCustomerAccessAsync(customerIdForOrder, cancellationToken))
        {
            return Forbid();
        }

        // Fetch all service requests for this order via query interface
        var services = await serviceRequestQueries.GetByOrderIdAsync(targetOrderId, cancellationToken);

        // Calculate counts
        var totalServices = services.Count;
        var completedServices = services.Count(s =>
            s.Status == ServiceStatus.Completed || s.Status == ServiceStatus.Canceled);
        var pendingServices = services.Count(s => s.Status == ServiceStatus.Pending);
        var failedServices = services.Count(s => s.Status == ServiceStatus.Failed);

        var result = new OrderServicesDto(
            targetOrderId,
            services,
            totalServices,
            completedServices,
            pendingServices,
            failedServices
        );

        return Ok(result);
    }

    [HttpGet("{orderId}/events")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<ActionResult<IReadOnlyCollection<OrderAuditEventDto>>> GetEventsAsync(
        string orderId,
        [FromQuery] int limit = 100,
        CancellationToken cancellationToken = default
    )
    {
        if (!Ulid.TryParse(orderId, out var parsedOrder))
        {
            return BadRequest("Invalid order id format.");
        }

        var targetOrderId = parsedOrder.ToString();

        // Verify order exists and user has access
        var customerIdForOrder = await orderQueries.GetCustomerIdAsync(targetOrderId, cancellationToken);

        if (customerIdForOrder is null)
        {
            return NotFound();
        }

        if (!await currentUserAccess.HasCustomerAccessAsync(customerIdForOrder, cancellationToken))
        {
            return Forbid();
        }

        // Read events from the event store for this order's stream
        var streamId = $"Order:{targetOrderId}";
        var clampedLimit = Math.Clamp(limit, 1, 500);

        var events = await eventStore.ReadStreamAsync(
            "*", // tenant - using wildcard for now
            streamId,
            0, // from position 0 (all events)
            clampedLimit,
            cancellationToken);

        var dtos = events.Select(e => new OrderAuditEventDto(
                e.Position,
                e.Version,
                e.EventId,
                e.EventName,
                JsonDocument.Parse(e.Payload).RootElement,
                new DateTimeOffset(e.CreatedAt, TimeSpan.Zero),
                e.CorrelationId,
                e.ActorId
            ))
            .ToList();

        return Ok(dtos);
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