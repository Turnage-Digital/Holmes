using System.Text.Json;
using Holmes.App.Infrastructure.Security;
using Holmes.App.Integration.Commands;
using Holmes.App.Server.Contracts;
using Holmes.Core.Application.Abstractions.Events;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Queries;
using Holmes.Orders.Application.Abstractions;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Queries;
using Holmes.Services.Domain;
using Holmes.Subjects.Application.Queries;
using Holmes.Orders.Application.Abstractions.Dtos;
using Holmes.Orders.Application.Commands;
using Holmes.Orders.Application.Queries;
using Holmes.Orders.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/orders")]
public sealed class OrdersController(
    IMediator mediator,
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

        if (!await mediator.Send(new CheckCustomerExistsQuery(customerId), cancellationToken))
        {
            return NotFound($"Customer '{customerId}' not found.");
        }

        var subjectId = parsedSubject.ToString();
        if (!await mediator.Send(new CheckSubjectExistsQuery(subjectId), cancellationToken))
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

        var summaryResult = await mediator.Send(new GetOrderSummaryQuery(orderId.ToString()), cancellationToken);

        if (!summaryResult.IsSuccess)
        {
            return Problem("Failed to load created order.");
        }

        return Created($"/api/orders/{orderId}/timeline", summaryResult.Value);
    }

    /// <summary>
    ///     Creates an order with intake session in a single atomic operation.
    ///     This is the preferred method for creating orders as it:
    ///     - Reuses existing subjects by email
    ///     - Creates order and intake session atomically
    ///     - Uses the outbox pattern for reliable event dispatch
    /// </summary>
    [HttpPost("with-intake")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<ActionResult<CreateOrderWithIntakeResponse>> CreateWithIntakeAsync(
        [FromBody] CreateOrderWithIntakeRequest request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.SubjectEmail))
        {
            return BadRequest("Subject email is required.");
        }

        if (!Ulid.TryParse(request.CustomerId, out var parsedCustomer))
        {
            return BadRequest("Invalid customer id format.");
        }

        var customerId = parsedCustomer.ToString();
        if (!await currentUserAccess.HasCustomerAccessAsync(customerId, cancellationToken))
        {
            return Forbid();
        }

        if (!await mediator.Send(new CheckCustomerExistsQuery(customerId), cancellationToken))
        {
            return NotFound($"Customer '{customerId}' not found.");
        }

        var command = new CreateOrderWithIntakeCommand(
            request.SubjectEmail.Trim(),
            request.SubjectPhone?.Trim(),
            UlidId.FromUlid(parsedCustomer),
            request.PolicySnapshotId);

        var result = await mediator.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        var response = new CreateOrderWithIntakeResponse(
            result.Value.SubjectId.ToString(),
            result.Value.SubjectWasExisting,
            result.Value.OrderId.ToString(),
            result.Value.IntakeSessionId.ToString(),
            result.Value.ResumeToken,
            result.Value.ExpiresAt);

        return Created($"/api/orders/{result.Value.OrderId}/timeline", response);
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

        var queryResult = await mediator.Send(
            new ListOrderSummariesQuery(filter, page, pageSize), cancellationToken);

        if (!queryResult.IsSuccess)
        {
            return Problem(queryResult.Error);
        }

        return Ok(PaginatedResponse<OrderSummaryDto>.Create(
            queryResult.Value.Items.ToList(), page, pageSize, queryResult.Value.TotalCount));
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

        var result = await mediator.Send(new GetOrderStatsQuery(allowedCustomers), cancellationToken);
        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
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

        var customerIdForOrder = await mediator.Send(
            new GetOrderCustomerIdQuery(targetOrderId), cancellationToken);

        if (customerIdForOrder is null)
        {
            return NotFound();
        }

        if (!await currentUserAccess.HasCustomerAccessAsync(customerIdForOrder, cancellationToken))
        {
            return Forbid();
        }

        var limit = Math.Clamp(query.Limit, 1, 200);
        var result = await mediator.Send(
            new GetOrderTimelineQuery(targetOrderId, query.Before, limit), cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
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
        var customerIdForOrder = await mediator.Send(
            new GetOrderCustomerIdQuery(targetOrderId), cancellationToken);

        if (customerIdForOrder is null)
        {
            return NotFound();
        }

        if (!await currentUserAccess.HasCustomerAccessAsync(customerIdForOrder, cancellationToken))
        {
            return Forbid();
        }

        // Fetch all services for this order via MediatR query
        var servicesResult = await mediator.Send(
            new GetServicesByOrderQuery(UlidId.Parse(targetOrderId)), cancellationToken);

        if (!servicesResult.IsSuccess)
        {
            return Problem(servicesResult.Error);
        }

        var services = servicesResult.Value;

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
        var customerIdForOrder = await mediator.Send(
            new GetOrderCustomerIdQuery(targetOrderId), cancellationToken);

        if (customerIdForOrder is null)
        {
            return NotFound();
        }

        if (!await currentUserAccess.HasCustomerAccessAsync(customerIdForOrder, cancellationToken))
        {
            return Forbid();
        }

        // Read events from the event store for this order's stream (special case - direct IEventStore)
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

    public sealed record CreateOrderWithIntakeRequest(
        string SubjectEmail,
        string? SubjectPhone,
        string CustomerId,
        string PolicySnapshotId
    );

    public sealed record CreateOrderWithIntakeResponse(
        string SubjectId,
        bool SubjectWasExisting,
        string OrderId,
        string IntakeSessionId,
        string ResumeToken,
        DateTimeOffset ExpiresAt
    );
}