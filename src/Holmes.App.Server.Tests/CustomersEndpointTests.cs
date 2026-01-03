using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Contracts.Dtos;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.Services.Contracts.Dtos;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class CustomersEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task CreateCustomer_Forbids_NonAdmin()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "user-no-admin");
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "user@holmes.dev");

        await CreateUserAsync(factory, "user-no-admin", "user@holmes.dev");
        await client.GetAsync("/api/users/me");

        var response = await client.PostAsJsonAsync("/api/customers", BuildCreateCustomerRequest("Acme"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Admin_Can_Create_And_Assign_Customer()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "admin-user");
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "admin@holmes.dev");

        var adminId = await PromoteCurrentUserToAdminAsync(factory);

        var createResponse =
            await client.PostAsJsonAsync("/api/customers", BuildCreateCustomerRequest("Acme Industries"));
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            TestContext.WriteLine(await createResponse.Content.ReadAsStringAsync());
        }

        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var summary = await createResponse.Content.ReadFromJsonAsync<CustomerListItemDto>(JsonOptions);
        Assert.That(summary, Is.Not.Null);
        var customerId = summary!.Id;

        var targetUserId = await CreateUserAsync(factory, "customer-admin", "cust.admin@holmes.dev");

        var assignResponse = await client.PostAsJsonAsync($"/api/customers/{customerId}/admins",
            new ModifyCustomerAdminRequest(targetUserId));
        Assert.That(assignResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/customers/{customerId}/admins")
        {
            Content = JsonContent.Create(new ModifyCustomerAdminRequest(targetUserId))
        };
        var revokeResponse = await client.SendAsync(revokeRequest);
        Assert.That(revokeResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        using var scope = factory.Services.CreateScope();
        var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
        var directory = await customersDb.CustomerProjections.AsNoTracking()
            .SingleAsync(c => c.CustomerId == customerId);
        Assert.That(directory.AdminCount, Is.EqualTo(1));
    }

    private static async Task<string> CreateUserAsync(HolmesWebApplicationFactory factory, string subject, string email)
    {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var id = await mediator.Send(new RegisterExternalUserCommand(
            "https://issuer.holmes.test",
            subject,
            email,
            subject,
            "pwd",
            DateTimeOffset.UtcNow,
            true)
        {
            UserId = SystemActors.System
        });
        return id.ToString();
    }

    private static async Task<UlidId> PromoteCurrentUserToAdminAsync(HolmesWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var id = await mediator.Send(new RegisterExternalUserCommand(
            "https://issuer.holmes.test",
            "admin-user",
            "admin@holmes.dev",
            "Admin User",
            "pwd",
            DateTimeOffset.UtcNow,
            true)
        {
            UserId = SystemActors.System
        });

        var grant = new GrantUserRoleCommand(
            id,
            UserRole.Admin,
            null,
            DateTimeOffset.UtcNow)
        {
            UserId = id.ToString()
        };
        await mediator.Send(grant);

        return id;
    }

    private static CreateCustomerRequest BuildCreateCustomerRequest(string name)
    {
        return new CreateCustomerRequest(
            name,
            "policy-dev",
            "billing@holmes.dev",
            [
                new CreateCustomerContactRequest("Ops Contact", "ops@holmes.dev", null, "Ops")
            ]);
    }

    private sealed record CreateCustomerRequest(
        string Name,
        string PolicySnapshotId,
        string? BillingEmail,
        IReadOnlyCollection<CreateCustomerContactRequest>? Contacts
    );

    private sealed record CreateCustomerContactRequest(
        string Name,
        string Email,
        string? Phone,
        string? Role
    );

    private sealed record ModifyCustomerAdminRequest(string UserId);

    // ==========================================================================
    // Service Catalog Tests
    // ==========================================================================

    [Test]
    public async Task GetServiceCatalog_Returns_Catalog_For_Customer()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "catalog-admin", "catalog-admin@holmes.dev");

        await PromoteUserToAdminAsync(factory, "catalog-admin", "catalog-admin@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerId);

        var response = await client.GetAsync($"/api/customers/{customerId}/service-catalog");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var catalog = await response.Content.ReadFromJsonAsync<CustomerServiceCatalogDto>(JsonOptions);
        Assert.That(catalog, Is.Not.Null);
        Assert.That(catalog!.CustomerId, Is.EqualTo(customerId));
        Assert.That(catalog.Services, Is.Not.Empty);
        Assert.That(catalog.Tiers, Is.Not.Empty);
    }

    [Test]
    public async Task GetServiceCatalog_Enforces_Customer_Access()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "catalog-viewer", "catalog-viewer@holmes.dev");

        var userId = await CreateUserIdAsync(factory, "catalog-viewer", "catalog-viewer@holmes.dev");
        var adminId = await PromoteUserToAdminAsync(factory, "catalog-admin-seed", "catalog-admin-seed@holmes.dev");

        var allowedCustomer = Ulid.NewUlid().ToString();
        var deniedCustomer = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, allowedCustomer);
        await SeedCustomerAsync(factory, deniedCustomer);
        await AssignCustomerAdminAsync(factory, allowedCustomer, userId, adminId);

        var allowedResponse = await client.GetAsync($"/api/customers/{allowedCustomer}/service-catalog");
        Assert.That(allowedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var deniedResponse = await client.GetAsync($"/api/customers/{deniedCustomer}/service-catalog");
        Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetServiceCatalog_Returns_NotFound_For_NonExistent_Customer()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "catalog-admin-nf", "catalog-admin-nf@holmes.dev");

        await PromoteUserToAdminAsync(factory, "catalog-admin-nf", "catalog-admin-nf@holmes.dev");

        var nonExistentCustomerId = Ulid.NewUlid().ToString();
        var response = await client.GetAsync($"/api/customers/{nonExistentCustomerId}/service-catalog");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task GetServiceCatalog_Returns_BadRequest_For_Invalid_CustomerId()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "catalog-admin-bad", "catalog-admin-bad@holmes.dev");

        await PromoteUserToAdminAsync(factory, "catalog-admin-bad", "catalog-admin-bad@holmes.dev");

        var response = await client.GetAsync("/api/customers/not-a-ulid/service-catalog");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task UpdateServiceCatalog_Updates_Catalog_For_Customer()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "catalog-ops", "catalog-ops@holmes.dev", "Operations");

        await PromoteUserToAdminAsync(factory, "catalog-ops", "catalog-ops@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerId);

        var updateRequest = new UpdateServiceCatalogRequest(
            [
                new ServiceCatalogServiceItem("SSN_TRACE", false, 1, null),
                new ServiceCatalogServiceItem("NATL_CRIM", true, 2, "VENDOR_A")
            ],
            [
                new ServiceCatalogTierRequestItem(1, "Custom Tier 1", "My custom tier", ["SSN_TRACE"], [], true, false)
            ]);

        var response = await client.PutAsJsonAsync($"/api/customers/{customerId}/service-catalog", updateRequest);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify the catalog was updated
        var getResponse = await client.GetAsync($"/api/customers/{customerId}/service-catalog");
        Assert.That(getResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var catalog = await getResponse.Content.ReadFromJsonAsync<CustomerServiceCatalogDto>(JsonOptions);
        Assert.That(catalog, Is.Not.Null);

        var ssnTrace = catalog!.Services.FirstOrDefault(s => s.ServiceTypeCode == "SSN_TRACE");
        Assert.That(ssnTrace, Is.Not.Null);
        Assert.That(ssnTrace!.IsEnabled, Is.False);

        var natlCrim = catalog.Services.FirstOrDefault(s => s.ServiceTypeCode == "NATL_CRIM");
        Assert.That(natlCrim, Is.Not.Null);
        Assert.That(natlCrim!.Tier, Is.EqualTo(2));
        Assert.That(natlCrim.VendorCode, Is.EqualTo("VENDOR_A"));

        var tier1 = catalog.Tiers.FirstOrDefault(t => t.Tier == 1);
        Assert.That(tier1, Is.Not.Null);
        Assert.That(tier1!.Name, Is.EqualTo("Custom Tier 1"));
    }

    [Test]
    public async Task UpdateServiceCatalog_Requires_Ops_Role()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "catalog-no-ops", "catalog-no-ops@holmes.dev", "Viewer"); // No Operations role

        // Just create user without Admin role - middleware enrichment will NOT add Admin role
        await CreateUserIdAsync(factory, "catalog-no-ops", "catalog-no-ops@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerId);

        var updateRequest = new UpdateServiceCatalogRequest(
            [new ServiceCatalogServiceItem("SSN_TRACE", false, 1, null)],
            null);

        var response = await client.PutAsJsonAsync($"/api/customers/{customerId}/service-catalog", updateRequest);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task UpdateServiceCatalog_Enforces_Customer_Access()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "catalog-ops-limited", "catalog-ops-limited@holmes.dev", "Operations");

        var userId = await CreateUserIdAsync(factory, "catalog-ops-limited", "catalog-ops-limited@holmes.dev");
        var adminId = await PromoteUserToAdminAsync(factory, "catalog-admin-seed-2", "catalog-admin-seed-2@holmes.dev");

        var allowedCustomer = Ulid.NewUlid().ToString();
        var deniedCustomer = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, allowedCustomer);
        await SeedCustomerAsync(factory, deniedCustomer);
        await AssignCustomerAdminAsync(factory, allowedCustomer, userId, adminId);

        var updateRequest = new UpdateServiceCatalogRequest(
            [new ServiceCatalogServiceItem("SSN_TRACE", false, 1, null)],
            null);

        var allowedResponse =
            await client.PutAsJsonAsync($"/api/customers/{allowedCustomer}/service-catalog", updateRequest);
        Assert.That(allowedResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var deniedResponse =
            await client.PutAsJsonAsync($"/api/customers/{deniedCustomer}/service-catalog", updateRequest);
        Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task UpdateServiceCatalog_Returns_NotFound_For_NonExistent_Customer()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "catalog-ops-nf", "catalog-ops-nf@holmes.dev", "Operations");

        await PromoteUserToAdminAsync(factory, "catalog-ops-nf", "catalog-ops-nf@holmes.dev");

        var nonExistentCustomerId = Ulid.NewUlid().ToString();
        var updateRequest = new UpdateServiceCatalogRequest(
            [new ServiceCatalogServiceItem("SSN_TRACE", false, 1, null)],
            null);

        var response =
            await client.PutAsJsonAsync($"/api/customers/{nonExistentCustomerId}/service-catalog", updateRequest);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task UpdateServiceCatalog_Returns_BadRequest_For_Empty_Request()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "catalog-ops-empty", "catalog-ops-empty@holmes.dev", "Operations");

        await PromoteUserToAdminAsync(factory, "catalog-ops-empty", "catalog-ops-empty@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerId);

        var updateRequest = new UpdateServiceCatalogRequest([], []);

        var response = await client.PutAsJsonAsync($"/api/customers/{customerId}/service-catalog", updateRequest);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    // ==========================================================================
    // Helper Methods for Service Catalog Tests
    // ==========================================================================

    private static void SetDefaultAuth(HttpClient client, string subject, string email, string? roles = null)
    {
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", subject);
        client.DefaultRequestHeaders.Add("X-Auth-Email", email);
        if (!string.IsNullOrWhiteSpace(roles))
        {
            client.DefaultRequestHeaders.Add("X-Auth-Roles", roles);
        }
    }

    private static async Task<UlidId> CreateUserIdAsync(
        HolmesWebApplicationFactory factory,
        string subject,
        string email
    )
    {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(new RegisterExternalUserCommand(
            "https://issuer.holmes.test",
            subject,
            email,
            subject,
            "pwd",
            DateTimeOffset.UtcNow,
            true)
        {
            UserId = SystemActors.System
        });
    }

    private static async Task<UlidId> PromoteUserToAdminAsync(
        HolmesWebApplicationFactory factory,
        string subject,
        string email
    )
    {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        var id = await mediator.Send(new RegisterExternalUserCommand(
            "https://issuer.holmes.test",
            subject,
            email,
            subject,
            "pwd",
            DateTimeOffset.UtcNow,
            true)
        {
            UserId = SystemActors.System
        });

        var grant = new GrantUserRoleCommand(id, UserRole.Admin, null, DateTimeOffset.UtcNow)
        {
            UserId = id.ToString()
        };
        await mediator.Send(grant);
        return id;
    }

    private static async Task SeedCustomerAsync(
        HolmesWebApplicationFactory factory,
        string customerId
    )
    {
        using var scope = factory.Services.CreateScope();
        var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
        customersDb.Customers.Add(new CustomerDb
        {
            CustomerId = customerId,
            Name = $"Customer-{customerId}",
            Status = CustomerStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow
        });

        customersDb.CustomerProfiles.Add(new CustomerProfileDb
        {
            CustomerId = customerId,
            PolicySnapshotId = "policy-dev",
            BillingEmail = null,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow
        });

        customersDb.CustomerProjections.Add(new CustomerProjectionDb
        {
            CustomerId = customerId,
            Name = $"Customer {customerId}",
            Status = CustomerStatus.Active,
            CreatedAt = DateTimeOffset.UtcNow,
            AdminCount = 0
        });

        await customersDb.SaveChangesAsync();
    }

    private static async Task AssignCustomerAdminAsync(
        HolmesWebApplicationFactory factory,
        string customerId,
        UlidId userId,
        UlidId assignedBy
    )
    {
        using var scope = factory.Services.CreateScope();
        var customersDb = scope.ServiceProvider.GetRequiredService<CustomersDbContext>();
        customersDb.CustomerAdmins.Add(new CustomerAdminDb
        {
            CustomerId = customerId,
            UserId = userId.ToString(),
            AssignedBy = assignedBy,
            AssignedAt = DateTimeOffset.UtcNow
        });

        var directory = await customersDb.CustomerProjections.SingleAsync(d => d.CustomerId == customerId);
        directory.AdminCount += 1;
        await customersDb.SaveChangesAsync();
    }

    private sealed record UpdateServiceCatalogRequest(
        IReadOnlyCollection<ServiceCatalogServiceItem>? Services,
        IReadOnlyCollection<ServiceCatalogTierRequestItem>? Tiers
    );

    private sealed record ServiceCatalogServiceItem(
        string ServiceTypeCode,
        bool IsEnabled,
        int Tier,
        string? VendorCode
    );

    private sealed record ServiceCatalogTierRequestItem(
        int Tier,
        string Name,
        string? Description,
        IReadOnlyCollection<string>? RequiredServices,
        IReadOnlyCollection<string>? OptionalServices,
        bool AutoDispatch,
        bool WaitForPreviousTier
    );
}
