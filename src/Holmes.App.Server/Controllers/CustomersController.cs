using System.Text.Json;
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
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql;
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
    ServicesDbContext servicesDbContext,
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
                return CustomerMapper.ToListItem(directory, profile, contactList ?? []);
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

        // Check customer exists
        var customerExists = await customersDbContext.CustomerDirectory
            .AsNoTracking()
            .AnyAsync(c => c.CustomerId == customerId, cancellationToken);

        if (!customerExists)
        {
            return NotFound();
        }

        // Get the latest catalog snapshot
        var latestSnapshot = await servicesDbContext.ServiceCatalogSnapshots
            .AsNoTracking()
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        if (latestSnapshot is null)
        {
            // Return default catalog with all services enabled
            return Ok(BuildDefaultCatalog(customerId));
        }

        // Parse the catalog config
        var catalog = ParseCatalogFromSnapshot(customerId, latestSnapshot);
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

        // Get or create catalog snapshot
        var latestSnapshot = await servicesDbContext.ServiceCatalogSnapshots
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var catalog = latestSnapshot is not null
            ? ParseCatalogConfig(latestSnapshot.ConfigJson)
            : BuildDefaultCatalogConfig();

        // Update the service in the catalog
        var serviceIndex = catalog.Services.FindIndex(s =>
            s.ServiceTypeCode.Equals(request.ServiceTypeCode, StringComparison.OrdinalIgnoreCase));

        if (serviceIndex < 0)
        {
            return BadRequest($"Service type '{request.ServiceTypeCode}' not found in catalog.");
        }

        catalog.Services[serviceIndex] = catalog.Services[serviceIndex] with
        {
            IsEnabled = request.IsEnabled,
            Tier = request.Tier ?? catalog.Services[serviceIndex].Tier,
            VendorCode = request.VendorCode ?? catalog.Services[serviceIndex].VendorCode
        };

        // Save new version
        await SaveCatalogSnapshotAsync(customerId, catalog, latestSnapshot?.Version ?? 0, cancellationToken);

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

        // Get or create catalog snapshot
        var latestSnapshot = await servicesDbContext.ServiceCatalogSnapshots
            .Where(s => s.CustomerId == customerId)
            .OrderByDescending(s => s.Version)
            .FirstOrDefaultAsync(cancellationToken);

        var catalog = latestSnapshot is not null
            ? ParseCatalogConfig(latestSnapshot.ConfigJson)
            : BuildDefaultCatalogConfig();

        // Update the tier configuration
        var tierIndex = catalog.Tiers.FindIndex(t => t.Tier == request.Tier);

        if (tierIndex < 0)
        {
            return BadRequest($"Tier {request.Tier} not found in catalog.");
        }

        var existingTier = catalog.Tiers[tierIndex];
        catalog.Tiers[tierIndex] = existingTier with
        {
            Name = request.Name ?? existingTier.Name,
            Description = request.Description ?? existingTier.Description,
            RequiredServices = request.RequiredServices ?? existingTier.RequiredServices,
            OptionalServices = request.OptionalServices ?? existingTier.OptionalServices,
            AutoDispatch = request.AutoDispatch ?? existingTier.AutoDispatch,
            WaitForPreviousTier = request.WaitForPreviousTier ?? existingTier.WaitForPreviousTier
        };

        // Save new version
        await SaveCatalogSnapshotAsync(customerId, catalog, latestSnapshot?.Version ?? 0, cancellationToken);

        return NoContent();
    }

    private static CustomerServiceCatalogDto BuildDefaultCatalog(string customerId)
    {
        var config = BuildDefaultCatalogConfig();

        return new CustomerServiceCatalogDto(
            customerId,
            config.Services.Select(s => new CatalogServiceItemDto(
                s.ServiceTypeCode,
                s.DisplayName,
                s.Category,
                s.IsEnabled,
                s.Tier,
                s.VendorCode
            )).ToList(),
            config.Tiers.Select(t => new TierConfigurationDto(
                t.Tier,
                t.Name,
                t.Description,
                t.RequiredServices,
                t.OptionalServices,
                t.AutoDispatch,
                t.WaitForPreviousTier
            )).ToList(),
            DateTimeOffset.UtcNow
        );
    }

    private static CatalogConfig BuildDefaultCatalogConfig()
    {
        var services = new List<ServiceConfig>
        {
            new("SSN_TRACE", "SSN Trace", ServiceCategory.Identity, true, 1, null),
            new("NATL_CRIM", "National Criminal Search", ServiceCategory.Criminal, true, 1, null),
            new("STATE_CRIM", "State Criminal Search", ServiceCategory.Criminal, true, 2, null),
            new("COUNTY_CRIM", "County Criminal Search", ServiceCategory.Criminal, true, 2, null),
            new("FED_CRIM", "Federal Criminal Search", ServiceCategory.Criminal, true, 2, null),
            new("SEX_OFFENDER", "Sex Offender Registry", ServiceCategory.Criminal, true, 1, null),
            new("GLOBAL_WATCH", "Global Watchlist Search", ServiceCategory.Criminal, true, 1, null),
            new("MVR", "Motor Vehicle Report", ServiceCategory.Driving, true, 2, null),
            new("CREDIT_CHECK", "Credit Report", ServiceCategory.Credit, false, 3, null),
            new("TWN_EMP", "Employment Verification (TWN)", ServiceCategory.Employment, true, 3, null),
            new("MANUAL_EMP", "Employment Verification (Manual)", ServiceCategory.Employment, false, 3, null),
            new("EDU_VERIFY", "Education Verification", ServiceCategory.Education, true, 3, null),
            new("PRO_LICENSE", "Professional License Verification", ServiceCategory.Employment, false, 3, null),
            new("REF_CHECK", "Reference Check", ServiceCategory.Reference, false, 4, null),
            new("DRUG_5", "5-Panel Drug Screen", ServiceCategory.Drug, false, 4, null),
            new("DRUG_10", "10-Panel Drug Screen", ServiceCategory.Drug, false, 4, null),
            new("CIVIL_COURT", "Civil Court Search", ServiceCategory.Civil, false, 2, null),
            new("BANKRUPTCY", "Bankruptcy Search", ServiceCategory.Civil, false, 2, null),
            new("HEALTHCARE_SANCTION", "Healthcare Sanctions Check", ServiceCategory.Healthcare, false, 2, null),
        };

        var tiers = new List<TierConfig>
        {
            new(1, "Identity & Preliminary Checks", "Initial identity verification and basic searches",
                ["SSN_TRACE", "NATL_CRIM", "SEX_OFFENDER", "GLOBAL_WATCH"], [], true, false),
            new(2, "Criminal & Driving", "Detailed criminal and motor vehicle checks",
                ["STATE_CRIM", "FED_CRIM"], ["COUNTY_CRIM", "MVR", "CIVIL_COURT", "BANKRUPTCY", "HEALTHCARE_SANCTION"], true, true),
            new(3, "Employment & Education", "Employment and education verifications",
                [], ["TWN_EMP", "MANUAL_EMP", "EDU_VERIFY", "PRO_LICENSE", "CREDIT_CHECK"], false, true),
            new(4, "Additional Checks", "Drug screening and reference checks",
                [], ["REF_CHECK", "DRUG_5", "DRUG_10"], false, true),
        };

        return new CatalogConfig(services, tiers);
    }

    private static CustomerServiceCatalogDto ParseCatalogFromSnapshot(
        string customerId,
        Holmes.Services.Infrastructure.Sql.Entities.ServiceCatalogSnapshotDb snapshot)
    {
        var config = ParseCatalogConfig(snapshot.ConfigJson);

        return new CustomerServiceCatalogDto(
            customerId,
            config.Services.Select(s => new CatalogServiceItemDto(
                s.ServiceTypeCode,
                s.DisplayName,
                s.Category,
                s.IsEnabled,
                s.Tier,
                s.VendorCode
            )).ToList(),
            config.Tiers.Select(t => new TierConfigurationDto(
                t.Tier,
                t.Name,
                t.Description,
                t.RequiredServices,
                t.OptionalServices,
                t.AutoDispatch,
                t.WaitForPreviousTier
            )).ToList(),
            new DateTimeOffset(snapshot.CreatedAt, TimeSpan.Zero)
        );
    }

    private static CatalogConfig ParseCatalogConfig(string configJson)
    {
        return JsonSerializer.Deserialize<CatalogConfig>(configJson, _jsonOptions)
            ?? BuildDefaultCatalogConfig();
    }

    private async Task SaveCatalogSnapshotAsync(
        string customerId,
        CatalogConfig config,
        int currentVersion,
        CancellationToken cancellationToken)
    {
        var caller = await currentUserAccess.GetUserIdAsync(cancellationToken);

        var snapshot = new Holmes.Services.Infrastructure.Sql.Entities.ServiceCatalogSnapshotDb
        {
            Id = Ulid.NewUlid().ToString(),
            CustomerId = customerId,
            Version = currentVersion + 1,
            ConfigJson = JsonSerializer.Serialize(config, _jsonOptions),
            CreatedAt = DateTime.UtcNow,
            CreatedBy = caller.ToString()
        };

        servicesDbContext.ServiceCatalogSnapshots.Add(snapshot);
        await servicesDbContext.SaveChangesAsync(cancellationToken);
    }

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    // Internal records for catalog configuration storage
    private sealed record ServiceConfig(
        string ServiceTypeCode,
        string DisplayName,
        ServiceCategory Category,
        bool IsEnabled,
        int Tier,
        string? VendorCode
    );

    private sealed record TierConfig(
        int Tier,
        string Name,
        string? Description,
        IReadOnlyCollection<string> RequiredServices,
        IReadOnlyCollection<string> OptionalServices,
        bool AutoDispatch,
        bool WaitForPreviousTier
    );

    private sealed record CatalogConfig(
        List<ServiceConfig> Services,
        List<TierConfig> Tiers
    );

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
        return CustomerMapper.ToListItem(directory, profile, contacts);
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