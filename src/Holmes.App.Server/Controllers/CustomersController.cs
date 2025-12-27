using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Abstractions.Commands;
using Holmes.Customers.Application.Abstractions.Dtos;
using Holmes.Customers.Application.Abstractions.Queries;
using Holmes.Services.Application.Abstractions.Dtos;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController(
    IMediator mediator,
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

        var result = await mediator.Send(
            new ListCustomersQuery(allowedCustomerIds, page, pageSize), cancellationToken);

        if (!result.IsSuccess)
        {
            return Problem(result.Error);
        }

        return Ok(PaginatedResponse<CustomerListItemDto>.Create(
            result.Value.Items.ToList(), page, pageSize, result.Value.TotalCount));
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

        var createdResult = await mediator.Send(
            new GetCustomerListItemQuery(id.ToString()), cancellationToken);

        if (!createdResult.IsSuccess)
        {
            return Problem("Failed to load created customer.");
        }

        return CreatedAtAction(nameof(GetCustomerById), new { customerId = id }, createdResult.Value);
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

        var result = await mediator.Send(new GetCustomerByIdQuery(customerId), cancellationToken);
        if (!result.IsSuccess)
        {
            return NotFound();
        }

        return Ok(result.Value);
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

    [HttpGet("{customerId}/service-catalog")]
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

        if (!await mediator.Send(new CheckCustomerExistsQuery(customerId), cancellationToken))
        {
            return NotFound();
        }

        var catalogResult = await mediator.Send(
            new GetCustomerServiceCatalogQuery(customerId), cancellationToken);

        if (!catalogResult.IsSuccess)
        {
            return Problem(catalogResult.Error);
        }

        return Ok(catalogResult.Value);
    }

    [HttpPut("{customerId}/service-catalog")]
    [Authorize(Policy = AuthorizationPolicies.RequireOps)]
    public async Task<IActionResult> UpdateServiceCatalog(
        string customerId,
        [FromBody] UpdateServiceCatalogRequest request,
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

        if (!await mediator.Send(new CheckCustomerExistsQuery(customerId), cancellationToken))
        {
            return NotFound();
        }

        var caller = await currentUserAccess.GetUserIdAsync(cancellationToken);

        var services = request.Services?
            .Select(s => new ServiceCatalogServiceInput(
                s.ServiceTypeCode,
                s.IsEnabled,
                s.Tier,
                s.VendorCode))
            .ToList() ?? [];

        var tiers = request.Tiers?
            .Select(t => new ServiceCatalogTierInput(
                t.Tier,
                t.Name,
                t.Description,
                t.RequiredServices ?? [],
                t.OptionalServices ?? [],
                t.AutoDispatch,
                t.WaitForPreviousTier))
            .ToList() ?? [];

        var result = await mediator.Send(new UpdateCustomerServiceCatalogCommand(
            customerId,
            services,
            tiers,
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

    public sealed record UpdateServiceCatalogRequest(
        IReadOnlyCollection<ServiceCatalogServiceItem>? Services,
        IReadOnlyCollection<ServiceCatalogTierRequestItem>? Tiers
    );

    public sealed record ServiceCatalogServiceItem(
        string ServiceTypeCode,
        bool IsEnabled,
        int Tier,
        string? VendorCode
    );

    public sealed record ServiceCatalogTierRequestItem(
        int Tier,
        string Name,
        string? Description,
        IReadOnlyCollection<string>? RequiredServices,
        IReadOnlyCollection<string>? OptionalServices,
        bool AutoDispatch,
        bool WaitForPreviousTier
    );
}