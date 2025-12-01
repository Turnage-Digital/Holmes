using Holmes.App.Infrastructure.Security;
using Holmes.App.Server.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Customers.Application.Abstractions.Dtos;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.Customers.Infrastructure.Sql.Mappers;
using Holmes.Customers.Infrastructure.Sql.Specifications;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public sealed class CustomersController(
    IMediator mediator,
    CustomersDbContext customersDbContext,
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
        IList<string>? allowedCustomerIds = null;
        if (!isGlobalAdmin)
        {
            allowedCustomerIds = (await currentUserAccess.GetAllowedCustomerIdsAsync(cancellationToken)).ToList();
        }

        var listingSpec = new CustomersVisibleToUserSpecification(allowedCustomerIds, page, pageSize);
        var countSpec = new CustomersVisibleToUserSpecification(allowedCustomerIds);

        var totalItems = await customersDbContext.CustomerDirectory
            .AsNoTracking()
            .ApplySpecification(countSpec)
            .CountAsync(cancellationToken);

        var directories = await customersDbContext.CustomerDirectory
            .AsNoTracking()
            .ApplySpecification(listingSpec)
            .ToListAsync(cancellationToken);

        var customerIdsPage = directories.Select(c => c.CustomerId).ToList();

        var profiles = await customersDbContext.CustomerProfiles
            .AsNoTracking()
            .Where(p => customerIdsPage.Contains(p.CustomerId))
            .ToDictionaryAsync(p => p.CustomerId, cancellationToken);

        var contacts = await customersDbContext.CustomerContacts
            .AsNoTracking()
            .Where(c => customerIdsPage.Contains(c.CustomerId))
            .GroupBy(c => c.CustomerId)
            .ToDictionaryAsync(
                g => g.Key,
                IReadOnlyCollection<CustomerContactDb> (g) => g.ToList(),
                cancellationToken);

        var items = directories
            .Select(directory =>
            {
                profiles.TryGetValue(directory.CustomerId, out var profile);
                contacts.TryGetValue(directory.CustomerId, out var contactList);
                return CustomerDtoMapper.ToListItem(directory, profile, contactList ?? []);
            })
            .ToList();

        return Ok(PaginatedResponse<CustomerListItemDto>.Create(items, page, pageSize, totalItems));
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

        await CreateCustomerProfileAsync(id.ToString(), request, timestamp, cancellationToken);

        var created = await LoadCustomerAsync(id.ToString(), cancellationToken);
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

        var customer = await LoadCustomerAsync(customerId, cancellationToken);
        if (customer is null)
        {
            return NotFound();
        }

        var admins = await customersDbContext.CustomerAdmins.AsNoTracking()
            .Where(a => a.CustomerId == customerId)
            .Select(a => new CustomerAdminDto(a.UserId, a.AssignedBy.ToString(), a.AssignedAt))
            .ToListAsync(cancellationToken);

        return Ok(new CustomerDetailDto(
            customer.Id,
            customer.TenantId,
            customer.Name,
            customer.Status,
            customer.PolicySnapshotId,
            customer.BillingEmail,
            customer.CreatedAt,
            customer.UpdatedAt,
            customer.Contacts,
            admins));
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

    private async Task<bool> HasCustomerAccessAsync(Ulid customerId, CancellationToken cancellationToken)
    {
        if (await currentUserAccess.IsGlobalAdminAsync(cancellationToken))
        {
            return true;
        }

        return await currentUserAccess.HasCustomerAccessAsync(customerId.ToString(), cancellationToken);
    }

    private async Task CreateCustomerProfileAsync(
        string customerId,
        CreateCustomerRequest request,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken
    )
    {
        var exists = await customersDbContext.CustomerProfiles
            .AnyAsync(p => p.CustomerId == customerId, cancellationToken);

        if (exists)
        {
            return;
        }

        var profile = new CustomerProfileDb
        {
            CustomerId = customerId,
            TenantId = Ulid.NewUlid().ToString(),
            PolicySnapshotId = string.IsNullOrWhiteSpace(request.PolicySnapshotId)
                ? "policy-default"
                : request.PolicySnapshotId.Trim(),
            BillingEmail = string.IsNullOrWhiteSpace(request.BillingEmail)
                ? null
                : request.BillingEmail.Trim(),
            CreatedAt = timestamp,
            UpdatedAt = timestamp
        };

        var contacts = (request.Contacts ?? [])
            .Where(c => !string.IsNullOrWhiteSpace(c.Name) && !string.IsNullOrWhiteSpace(c.Email))
            .Select(c => new CustomerContactDb
            {
                ContactId = Ulid.NewUlid().ToString(),
                CustomerId = customerId,
                Name = c.Name.Trim(),
                Email = c.Email.Trim(),
                Phone = string.IsNullOrWhiteSpace(c.Phone) ? null : c.Phone.Trim(),
                Role = string.IsNullOrWhiteSpace(c.Role) ? null : c.Role.Trim(),
                CreatedAt = timestamp
            })
            .ToList();

        customersDbContext.CustomerProfiles.Add(profile);
        if (contacts.Count > 0)
        {
            await customersDbContext.CustomerContacts.AddRangeAsync(contacts, cancellationToken);
        }

        await customersDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<CustomerListItemDto?> LoadCustomerAsync(
        string customerId,
        CancellationToken cancellationToken
    )
    {
        var directory = await customersDbContext.CustomerDirectory.AsNoTracking()
            .SingleOrDefaultAsync(c => c.CustomerId == customerId, cancellationToken);

        if (directory is null)
        {
            return null;
        }

        var profile = await customersDbContext.CustomerProfiles.AsNoTracking()
            .Include(p => p.Contacts)
            .SingleOrDefaultAsync(p => p.CustomerId == customerId, cancellationToken);

        var contacts = profile?.Contacts?.ToList() ?? [];
        return CustomerDtoMapper.ToListItem(directory, profile, contacts);
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
}