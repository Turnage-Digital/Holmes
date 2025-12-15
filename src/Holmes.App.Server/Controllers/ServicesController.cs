using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Application.Queries;
using Holmes.Services.Domain;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/services")]
public sealed class ServicesController(
    IMediator mediator,
    ICurrentUserAccess currentUserAccess
) : ControllerBase
{
    /// <summary>
    ///     Returns all available service types.
    /// </summary>
    [HttpGet("types")]
    public async Task<ActionResult<IReadOnlyCollection<ServiceTypeDto>>> GetServiceTypes(
        CancellationToken cancellationToken
    )
    {
        var result = await mediator.Send(new ListServiceTypesQuery(), cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(result.Value);
    }

    /// <summary>
    ///     Returns paginated service requests for the fulfillment dashboard queue.
    ///     Supports filtering by status and customer.
    /// </summary>
    [HttpGet("queue")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<ActionResult<PaginatedResponse<ServiceRequestSummaryDto>>> GetFulfillmentQueue(
        [FromQuery] ServiceQueueQuery query,
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
            return Ok(PaginatedResponse<ServiceRequestSummaryDto>.Create([], page, pageSize, 0));
        }

        // Validate customer ID if provided
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

        var filter = new ServiceFulfillmentQueueFilter(
            customerId is null ? allowedCustomers : null,
            customerId,
            NormalizeStatuses(query.Status),
            NormalizeCategories(query.Category));

        var queryResult = await mediator.Send(
            new GetServiceFulfillmentQueueQuery(filter, page, pageSize), cancellationToken);

        if (!queryResult.IsSuccess)
        {
            return Problem(queryResult.Error);
        }

        return Ok(PaginatedResponse<ServiceRequestSummaryDto>.Create(
            queryResult.Value.Items.ToList(), page, pageSize, queryResult.Value.TotalCount));
    }

    private static IReadOnlyCollection<ServiceStatus>? NormalizeStatuses(IReadOnlyCollection<string>? statuses)
    {
        if (statuses is null || statuses.Count is 0)
        {
            return null;
        }

        var normalized = new List<ServiceStatus>(statuses.Count);
        foreach (var candidate in statuses)
        {
            if (Enum.TryParse<ServiceStatus>(candidate, true, out var parsed))
            {
                normalized.Add(parsed);
            }
        }

        return normalized.Count > 0 ? normalized : null;
    }

    private static IReadOnlyCollection<ServiceCategory>? NormalizeCategories(IReadOnlyCollection<string>? categories)
    {
        if (categories is null || categories.Count is 0)
        {
            return null;
        }

        var normalized = new List<ServiceCategory>(categories.Count);
        foreach (var candidate in categories)
        {
            if (Enum.TryParse<ServiceCategory>(candidate, true, out var parsed))
            {
                normalized.Add(parsed);
            }
        }

        return normalized.Count > 0 ? normalized : null;
    }

    public sealed record ServiceQueueQuery
    {
        public int Page { get; init; } = 1;

        public int PageSize { get; init; } = 25;

        public string? CustomerId { get; init; }

        public IReadOnlyCollection<string>? Status { get; init; }

        public IReadOnlyCollection<string>? Category { get; init; }
    }
}