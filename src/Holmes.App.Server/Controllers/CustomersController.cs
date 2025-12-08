using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Abstractions.Dtos;
using Holmes.Customers.Application.Abstractions.Queries;
using Holmes.Customers.Application.Commands;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Application.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController(
    IMediator mediator,
    ICustomerQueries customerQueries,
    IServiceCatalogQueries serviceCatalogQueries,
    ICurrentUserAccess currentUserAccess
) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<PaginatedResponse<CustomerListItemDto>>> GetCustomers(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken
    )
    {
        var (page, pageSize) = PaginationNormalization.Normalize(query);
        var isGlobalAdmin = await currentUserAccess.IsGlobalAdminAsync(cancellationToken);
        IReadOnlyCollection<string>? allowedCustomerIds = null;
        if (!isGlobalAdmin)
        {
            allowedCustomerIds = await currentUserAccess.GetAllowedCustomerIdsAsync(cancellationToken);
        }

        var result = await customerQueries.GetCustomersPagedAsync(
            allowedCustomerIds, page, pageSize, cancellationToken);

        return Ok(PaginatedResponse<CustomerListItemDto>.Create(
            result.Items.ToList(), page, pageSize, result.TotalCount));
    }

    [HttpPost]
    public async Task<ActionResult<CustomerListItemDto>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken
    )
    {
        var caller = await currentUserAccess.GetUserIdAsync(cancellationToken);

        if (!await currentUserAccess.IsGlobalAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var timestamp = DateTimeOffset.UtcNow;

        var id = await mediator.Send(new RegisterCustomerCommand(request.Name, timestamp), cancellationToken);

        await mediator.Send(new AssignCustomerAdminCommand(
            id,
            caller,
            timestamp), cancellationToken);

        var contacts = request.Contacts?
            .Select(c => new CreateContactInfo(c.Name, c.Email, c.Phone, c.Role))
            .ToList();

        await mediator.Send(new CreateCustomerProfileCommand(
            id,
            request.PolicySnapshotId,
            request.BillingEmail,
            contacts,
            timestamp), cancellationToken);

        var created = await customerQueries.GetListItemByIdAsync(id.ToString(), cancellationToken);
        if (created is null)
        {
            return Problem("Failed to load created customer.");
        }

        return CreatedAtAction(nameof(GetCustomerById), new { customerId = id }, created);
    }

    [HttpGet("{customerId}")]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomerById(
        string customerId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsed))
        {
            return BadRequest("Invalid customer id format.");
        }

        if (!await HasCustomerAccessAsync(parsed, cancellationToken))
        {
            return Forbid();
        }

        var customer = await customerQueries.GetByIdAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        return Ok(customer);
    }

    [HttpPost("{customerId}/admins")]
    public async Task<IActionResult> AssignCustomerAdmin(
        string customerId,
        [FromBody] ModifyCustomerAdminRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsedCustomer) || !Ulid.TryParse(request.UserId, out var parsedUser))
        {
            return BadRequest("Invalid id format.");
        }

        if (!await HasCustomerAccessAsync(parsedCustomer, cancellationToken))
        {
            return Forbid();
        }

        var result = await mediator.Send(new AssignCustomerAdminCommand(
            UlidId.FromUlid(parsedCustomer),
            UlidId.FromUlid(parsedUser),
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    [HttpDelete("{customerId}/admins")]
    public async Task<IActionResult> RemoveCustomerAdmin(
        string customerId,
        [FromBody] ModifyCustomerAdminRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsedCustomer) || !Ulid.TryParse(request.UserId, out var parsedUser))
        {
            return BadRequest("Invalid id format.");
        }

        if (!await HasCustomerAccessAsync(parsedCustomer, cancellationToken))
        {
            return Forbid();
        }

        var result = await mediator.Send(new RemoveCustomerAdminCommand(
            UlidId.FromUlid(parsedCustomer),
            UlidId.FromUlid(parsedUser),
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    // ==========================================================================
    // Service Catalog
    // ==========================================================================

    [HttpGet("{customerId}/catalog")]
    public async Task<ActionResult<CustomerServiceCatalogDto>> GetServiceCatalog(
        string customerId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsed))
        {
            return BadRequest("Invalid customer id format.");
        }

        if (!await HasCustomerAccessAsync(parsed, cancellationToken))
        {
            return Forbid();
        }

        // Check customer exists via query interface
        if (!await customerQueries.ExistsAsync(customerId, cancellationToken))
        {
            return NotFound();
        }

        var catalog = await serviceCatalogQueries.GetByCustomerIdAsync(customerId, cancellationToken);
        return Ok(catalog);
    }

    [HttpPut("{customerId}/catalog/services")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<IActionResult> UpdateCatalogService(
        string customerId,
        [FromBody] UpdateCatalogServiceRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsed))
        {
            return BadRequest("Invalid customer id format.");
        }

        if (!await HasCustomerAccessAsync(parsed, cancellationToken))
        {
            return Forbid();
        }

        var caller = await currentUserAccess.GetUserIdAsync(cancellationToken);

        var result = await mediator.Send(new UpdateCatalogServiceCommand(
            customerId,
            request.ServiceTypeCode,
            request.IsEnabled,
            request.Tier,
            request.VendorCode,
            caller), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    [HttpPut("{customerId}/catalog/tiers")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<IActionResult> UpdateTierConfiguration(
        string customerId,
        [FromBody] UpdateTierConfigurationRequest request,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsed))
        {
            return BadRequest("Invalid customer id format.");
        }

        if (!await HasCustomerAccessAsync(parsed, cancellationToken))
        {
            return Forbid();
        }

        var caller = await currentUserAccess.GetUserIdAsync(cancellationToken);

        var result = await mediator.Send(new UpdateTierConfigurationCommand(
            customerId,
            request.Tier,
            request.Name,
            request.Description,
            request.RequiredServices,
            request.OptionalServices,
            request.AutoDispatch,
            request.WaitForPreviousTier,
            caller), cancellationToken);

        if (!result.IsSuccess)
        {
            return BadRequest(result.Error);
        }

        return NoContent();
    }

    private async Task<bool> HasCustomerAccessAsync(Ulid customerId, CancellationToken cancellationToken)
    {
        if (await currentUserAccess.IsGlobalAdminAsync(cancellationToken))
        {
            return true;
        }

        return await currentUserAccess.HasCustomerAccessAsync(customerId.ToString(), cancellationToken);
    }

    public sealed record CreateCustomerRequest(
        string Name,
        string PolicySnapshotId,
        string? BillingEmail,
        IReadOnlyCollection<CreateCustomerContactRequest>? Contacts
    );

    public sealed record CreateCustomerContactRequest(
        string Name,
        string Email,
        string? Phone,
        string? Role
    );

    public sealed record ModifyCustomerAdminRequest(string UserId);

    public sealed record UpdateCatalogServiceRequest(
        string ServiceTypeCode,
        bool IsEnabled,
        int? Tier,
        string? VendorCode
    );

    public sealed record UpdateTierConfigurationRequest(
        int Tier,
        string? Name,
        string? Description,
        IReadOnlyCollection<string>? RequiredServices,
        IReadOnlyCollection<string>? OptionalServices,
        bool? AutoDispatch,
        bool? WaitForPreviousTier
    );
}