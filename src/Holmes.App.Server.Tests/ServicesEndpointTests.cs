using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.App.Server.Contracts;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Domain;
using Holmes.Services.Infrastructure.Sql;
using Holmes.Services.Infrastructure.Sql.Entities;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Workflow.Domain;
using Holmes.Workflow.Infrastructure.Sql;
using Holmes.Workflow.Infrastructure.Sql.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class ServicesEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task GetQueue_Returns_Pending_Services()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "services-admin-queue", "services-admin-queue@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "services-admin-queue", "services-admin-queue@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerId, "tenant-services-queue");
        await SeedOrderSummaryAsync(factory, orderId, customerId);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, ServiceStatus.Pending);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, ServiceStatus.InProgress);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, ServiceStatus.Completed);

        // Filter by customerId to isolate test data
        var response = await client.GetAsync($"/api/services/queue?customerId={customerId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ServiceRequestSummaryDto>>(JsonOptions);
        Assert.That(result, Is.Not.Null);
        // Should only return pending and in-progress by default (not completed)
        Assert.That(result!.Items, Has.Count.EqualTo(2));
        Assert.That(result.Items.All(s => s.Status == ServiceStatus.Pending || s.Status == ServiceStatus.InProgress), Is.True);
    }

    [Test]
    public async Task GetQueue_Filters_By_Status()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "services-admin-status", "services-admin-status@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "services-admin-status", "services-admin-status@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerId, "tenant-services-status");
        await SeedOrderSummaryAsync(factory, orderId, customerId);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, ServiceStatus.Pending);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, ServiceStatus.InProgress);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, ServiceStatus.Failed);

        var response = await client.GetAsync("/api/services/queue?status=Failed");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ServiceRequestSummaryDto>>(JsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.First().Status, Is.EqualTo(ServiceStatus.Failed));
    }

    [Test]
    public async Task GetQueue_Filters_By_CustomerId()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "services-admin-cust", "services-admin-cust@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "services-admin-cust", "services-admin-cust@holmes.dev");

        var customerId1 = Ulid.NewUlid().ToString();
        var customerId2 = Ulid.NewUlid().ToString();
        var orderId1 = Ulid.NewUlid().ToString();
        var orderId2 = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, customerId1, "tenant-services-cust-1");
        await SeedCustomerAsync(factory, customerId2, "tenant-services-cust-2");
        await SeedOrderSummaryAsync(factory, orderId1, customerId1);
        await SeedOrderSummaryAsync(factory, orderId2, customerId2);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), orderId1, customerId1, ServiceStatus.Pending);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), orderId2, customerId2, ServiceStatus.Pending);

        var response = await client.GetAsync($"/api/services/queue?customerId={customerId1}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ServiceRequestSummaryDto>>(JsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.First().CustomerId, Is.EqualTo(customerId1));
    }

    [Test]
    public async Task GetQueue_Supports_Pagination()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "services-admin-page", "services-admin-page@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "services-admin-page", "services-admin-page@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerId, "tenant-services-page");
        await SeedOrderSummaryAsync(factory, orderId, customerId);

        // Seed 5 pending services
        for (var i = 0; i < 5; i++)
        {
            await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, ServiceStatus.Pending);
        }

        // Filter by customerId to isolate test data
        var response = await client.GetAsync($"/api/services/queue?customerId={customerId}&page=1&pageSize=2");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ServiceRequestSummaryDto>>(JsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items, Has.Count.EqualTo(2));
        Assert.That(result.TotalItems, Is.EqualTo(5));
        Assert.That(result.TotalPages, Is.EqualTo(3));
    }

    [Test]
    public async Task GetQueue_Enforces_Customer_Access()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "services-limited-queue", "services-limited-queue@holmes.dev", "Operations");

        var userId = await CreateUserAsync(factory, "services-limited-queue", "services-limited-queue@holmes.dev");
        var adminId = await PromoteCurrentUserToAdminAsync(factory, "services-admin-seed-queue", "admin-queue@holmes.dev");

        var allowedCustomer = Ulid.NewUlid().ToString();
        var deniedCustomer = Ulid.NewUlid().ToString();
        var allowedOrder = Ulid.NewUlid().ToString();
        var deniedOrder = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, allowedCustomer, "tenant-allowed-queue");
        await SeedCustomerAsync(factory, deniedCustomer, "tenant-denied-queue");
        await AssignCustomerAdminAsync(factory, allowedCustomer, userId, adminId);

        await SeedOrderSummaryAsync(factory, allowedOrder, allowedCustomer);
        await SeedOrderSummaryAsync(factory, deniedOrder, deniedCustomer);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), allowedOrder, allowedCustomer, ServiceStatus.Pending);
        await SeedServiceRequestAsync(factory, Ulid.NewUlid().ToString(), deniedOrder, deniedCustomer, ServiceStatus.Pending);

        // Should only return services from allowed customer
        var response = await client.GetAsync("/api/services/queue");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var result = await response.Content.ReadFromJsonAsync<PaginatedResponse<ServiceRequestSummaryDto>>(JsonOptions);
        Assert.That(result, Is.Not.Null);
        Assert.That(result!.Items, Has.Count.EqualTo(1));
        Assert.That(result.Items.First().CustomerId, Is.EqualTo(allowedCustomer));
    }

    [Test]
    public async Task GetQueue_Forbids_Access_To_Denied_Customer()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "services-denied-queue", "services-denied-queue@holmes.dev", "Operations");

        var userId = await CreateUserAsync(factory, "services-denied-queue", "services-denied-queue@holmes.dev");
        var adminId = await PromoteCurrentUserToAdminAsync(factory, "services-admin-seed-denied", "admin-denied@holmes.dev");

        var allowedCustomer = Ulid.NewUlid().ToString();
        var deniedCustomer = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, allowedCustomer, "tenant-allowed-denied");
        await SeedCustomerAsync(factory, deniedCustomer, "tenant-denied-denied");
        await AssignCustomerAdminAsync(factory, allowedCustomer, userId, adminId);

        // Trying to access denied customer directly should return Forbid
        var response = await client.GetAsync($"/api/services/queue?customerId={deniedCustomer}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetQueue_Returns_BadRequest_For_Invalid_CustomerId()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "services-admin-bad", "services-admin-bad@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "services-admin-bad", "services-admin-bad@holmes.dev");

        var response = await client.GetAsync("/api/services/queue?customerId=not-a-ulid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetQueue_Requires_Operations_Role()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        // Explicitly set non-Operations role (TestAuthenticationHandler defaults to Operations if header is missing)
        SetDefaultAuth(client, "services-viewer", "services-viewer@holmes.dev", "Viewer");

        await CreateUserAsync(factory, "services-viewer", "services-viewer@holmes.dev");

        var response = await client.GetAsync("/api/services/queue");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

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

    private static async Task<UlidId> CreateUserAsync(HolmesWebApplicationFactory factory, string subject, string email)
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
            true));
    }

    private static async Task<UlidId> PromoteCurrentUserToAdminAsync(
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
            true));

        var grant = new GrantUserRoleCommand(id, UserRole.Admin, null, DateTimeOffset.UtcNow)
        {
            UserId = id.ToString()
        };
        await mediator.Send(grant);
        return id;
    }

    private static async Task SeedCustomerAsync(
        HolmesWebApplicationFactory factory,
        string customerId,
        string tenantId
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
            TenantId = tenantId,
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

    private static async Task SeedOrderSummaryAsync(
        HolmesWebApplicationFactory factory,
        string orderId,
        string customerId
    )
    {
        using var scope = factory.Services.CreateScope();
        var workflowDb = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        workflowDb.OrderSummaries.Add(new OrderSummaryProjectionDb
        {
            OrderId = orderId,
            CustomerId = customerId,
            SubjectId = Ulid.NewUlid().ToString(),
            PolicySnapshotId = "policy-v1",
            PackageCode = null,
            Status = OrderStatus.ReadyForFulfillment.ToString(),
            LastStatusReason = "seeded",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            LastUpdatedAt = DateTimeOffset.UtcNow,
            ReadyForFulfillmentAt = DateTimeOffset.UtcNow,
            ClosedAt = null,
            CanceledAt = null
        });
        await workflowDb.SaveChangesAsync();
    }

    private static async Task SeedServiceRequestAsync(
        HolmesWebApplicationFactory factory,
        string serviceId,
        string orderId,
        string customerId,
        ServiceStatus status
    )
    {
        using var scope = factory.Services.CreateScope();
        var servicesDb = scope.ServiceProvider.GetRequiredService<ServicesDbContext>();

        var now = DateTime.UtcNow;
        var service = new ServiceRequestDb
        {
            Id = serviceId,
            OrderId = orderId,
            CustomerId = customerId,
            ServiceTypeCode = "CRIMINAL_COUNTY",
            Category = ServiceCategory.Criminal,
            Tier = 1,
            Status = status,
            VendorCode = "TEST_VENDOR",
            AttemptCount = 0,
            MaxAttempts = 3,
            CreatedAt = now,
            UpdatedAt = now,
            DispatchedAt = status >= ServiceStatus.Dispatched ? now : null,
            CompletedAt = status == ServiceStatus.Completed ? now : null,
            FailedAt = status == ServiceStatus.Failed ? now : null
        };

        servicesDb.ServiceRequests.Add(service);
        await servicesDb.SaveChangesAsync();
    }
}
