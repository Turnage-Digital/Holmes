using Holmes.App.Server.Contracts;
using Holmes.Core.Application;
using Holmes.Core.Application.Abstractions.Specifications;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Application.Abstractions.Dtos;
using Holmes.Customers.Application.Commands;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.Customers.Infrastructure.Sql.Specifications;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/customers")]
public class CustomersController(
    IMediator mediator,
    CustomersDbContext customersDbContext,
    UsersDbContext usersDbContext,
    ICurrentUserInitializer currentUserInitializer,
    ISpecificationQueryExecutor specificationExecutor
) : ControllerBase
{
    [HttpGet]
    [Authorize]
    public async Task<ActionResult<PaginatedResponse<CustomerListItemDto>>> GetCustomers(
        [FromQuery] PaginationQuery query,
        CancellationToken cancellationToken
    )
    {
        var caller = await EnsureUserAsync(cancellationToken);

        var (page, pageSize) = NormalizePagination(query);
        IList<string>? allowedCustomerIds = null;
        if (!await IsGlobalAdminAsync(caller, cancellationToken))
        {
            allowedCustomerIds = await customersDbContext.CustomerAdmins
                .AsNoTracking()
                .Where(a => a.UserId == caller.ToString())
                .Select(a => a.CustomerId)
                .ToListAsync(cancellationToken);
        }

        var listingSpec = new CustomersVisibleToUserSpecification(allowedCustomerIds, page, pageSize);
        var countSpec = new CustomersVisibleToUserSpecification(allowedCustomerIds);

        var totalItems = await specificationExecutor
            .Apply(customersDbContext.CustomerDirectory.AsNoTracking(), countSpec)
            .CountAsync(cancellationToken);

        var directories = await specificationExecutor
            .Apply(customersDbContext.CustomerDirectory.AsNoTracking(), listingSpec)
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
                return MapCustomer(directory, profile, contactList ?? []);
            })
            .ToList();

        return Ok(PaginatedResponse<CustomerListItemDto>.Create(items, page, pageSize, totalItems));
    }

    [HttpPost]
    [Authorize]
    public async Task<ActionResult<CustomerListItemDto>> CreateCustomer(
        [FromBody] CreateCustomerRequest request,
        CancellationToken cancellationToken
    )
    {
        var caller = await EnsureUserAsync(cancellationToken);

        if (!await IsGlobalAdminAsync(caller, cancellationToken))
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
    [Authorize]
    public async Task<ActionResult<CustomerDetailDto>> GetCustomerById(
        string customerId,
        CancellationToken cancellationToken
    )
    {
        if (!Ulid.TryParse(customerId, out var parsed))
        {
            return BadRequest("Invalid customer id format.");
        }

        var caller = await EnsureUserAsync(cancellationToken);
        if (!await HasCustomerAccessAsync(parsed, caller, cancellationToken))
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
    [Authorize]
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

        var caller = await EnsureUserAsync(cancellationToken);
        if (!await HasCustomerAccessAsync(parsedCustomer, caller, cancellationToken))
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
    [Authorize]
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

        var caller = await EnsureUserAsync(cancellationToken);
        if (!await HasCustomerAccessAsync(parsedCustomer, caller, cancellationToken))
        {
            return Forbid();
        }

        var result = await mediator.Send(new RemoveCustomerAdminCommand(
            UlidId.FromUlid(parsedCustomer),
            UlidId.FromUlid(parsedUser),
            DateTimeOffset.UtcNow), cancellationToken);

        return result.IsSuccess ? NoContent() : BadRequest(result.Error);
    }

    private async Task<bool> HasCustomerAccessAsync(Ulid customerId, UlidId caller, CancellationToken cancellationToken)
    {
        if (await IsGlobalAdminAsync(caller, cancellationToken))
        {
            return true;
        }

        return await customersDbContext.CustomerAdmins
            .AsNoTracking()
            .AnyAsync(a => a.CustomerId == customerId.ToString() && a.UserId == caller.ToString(), cancellationToken);
    }

    private async Task<bool> IsGlobalAdminAsync(UlidId caller, CancellationToken cancellationToken)
    {
        return await usersDbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(r =>
                    r.UserId == caller.ToString() &&
                    r.Role == UserRole.Admin &&
                    r.CustomerId == null,
                cancellationToken);
    }

    private async Task<UlidId> EnsureUserAsync(CancellationToken cancellationToken)
    {
        return await currentUserInitializer.EnsureCurrentUserIdAsync(cancellationToken);
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
        return MapCustomer(directory, profile, contacts);
    }

    private static (int Page, int PageSize) NormalizePagination(PaginationQuery query)
    {
        var page = query.Page <= 0 ? 1 : query.Page;
        var size = query.PageSize <= 0 ? 25 : Math.Min(query.PageSize, 100);
        return (page, size);
    }

    private static CustomerListItemDto MapCustomer(
        CustomerDirectoryDb directory,
        CustomerProfileDb? profile,
        IReadOnlyCollection<CustomerContactDb> contacts
    )
    {
        var policySnapshotId = string.IsNullOrWhiteSpace(profile?.PolicySnapshotId)
            ? "policy-default"
            : profile!.PolicySnapshotId;

        var billingEmail = string.IsNullOrWhiteSpace(profile?.BillingEmail) ? null : profile!.BillingEmail;

        var contactResponses = contacts
            .OrderBy(c => c.Name)
            .Select(c => new CustomerContactDto(
                c.ContactId,
                c.Name,
                c.Email,
                c.Phone,
                c.Role))
            .ToList();

        return new CustomerListItemDto(
            directory.CustomerId,
            profile?.TenantId ?? directory.CustomerId,
            directory.Name,
            directory.Status,
            policySnapshotId,
            billingEmail,
            contactResponses,
            directory.CreatedAt,
            profile?.UpdatedAt ?? directory.CreatedAt);
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