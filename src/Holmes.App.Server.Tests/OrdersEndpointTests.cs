using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.App.Server.Contracts;
using Holmes.App.Server.Controllers;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Entities;
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
public class OrdersEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task Admin_Receives_Paginated_Summaries()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "orders-admin", "admin@holmes.dev");

        await PromoteCurrentUserToAdminAsync(factory, "orders-admin", "admin@holmes.dev");
        await SeedOrderSummaryAsync(factory, Ulid.NewUlid().ToString(), Ulid.NewUlid().ToString(),
            Ulid.NewUlid().ToString(), OrderStatus.Invited);
        await SeedOrderSummaryAsync(factory, Ulid.NewUlid().ToString(), Ulid.NewUlid().ToString(),
            Ulid.NewUlid().ToString(), OrderStatus.ReadyForRouting);

        var response = await client.GetAsync("/api/orders/summary?page=1&pageSize=1");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var payload = await response.Content.ReadFromJsonAsync<PaginatedResponse<OrderSummaryResponse>>(JsonOptions);
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.TotalItems, Is.EqualTo(2));
        Assert.That(payload.PageSize, Is.EqualTo(1));
        Assert.That(payload.Items, Has.Count.EqualTo(1));
    }

    [Test]
    public async Task Summary_Filters_To_Assigned_Customers()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "orders-viewer", "viewer@holmes.dev");

        var viewerId = await CreateUserAsync(factory, "orders-viewer", "viewer@holmes.dev");
        var adminId = await PromoteCurrentUserToAdminAsync(factory, "admin-seed", "admin@holmes.dev");

        var customerA = Ulid.NewUlid().ToString();
        var customerB = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerA, "tenant-1");
        await SeedCustomerAsync(factory, customerB, "tenant-2");
        await AssignCustomerAdminAsync(factory, customerA, viewerId, adminId);

        var orderA = Ulid.NewUlid().ToString();
        await SeedOrderSummaryAsync(factory, orderA, customerA, Ulid.NewUlid().ToString(), OrderStatus.Invited);
        await SeedOrderSummaryAsync(factory, Ulid.NewUlid().ToString(), customerB, Ulid.NewUlid().ToString(),
            OrderStatus.ReadyForRouting);

        var response = await client.GetAsync("/api/orders/summary");
        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var payload = await response.Content.ReadFromJsonAsync<PaginatedResponse<OrderSummaryResponse>>(JsonOptions);
        Assert.That(payload, Is.Not.Null);
        var summary = payload!.Items.Single();
        Assert.That(summary.OrderId, Is.EqualTo(orderA));
    }

    [Test]
    public async Task Timeline_Enforces_Customer_Access()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "timeline-user", "timeline@holmes.dev");

        var userId = await CreateUserAsync(factory, "timeline-user", "timeline@holmes.dev");
        var adminId = await PromoteCurrentUserToAdminAsync(factory, "timeline-admin", "admin@holmes.dev");

        var customerAllowed = Ulid.NewUlid().ToString();
        var customerDenied = Ulid.NewUlid().ToString();
        var allowedOrder = Ulid.NewUlid().ToString();
        var deniedOrder = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, customerAllowed, "tenant-allowed");
        await SeedCustomerAsync(factory, customerDenied, "tenant-denied");
        await AssignCustomerAdminAsync(factory, customerAllowed, userId, adminId);

        await SeedOrderSummaryAsync(factory, allowedOrder, customerAllowed, Ulid.NewUlid().ToString(),
            OrderStatus.IntakeComplete);
        await SeedOrderSummaryAsync(factory, deniedOrder, customerDenied, Ulid.NewUlid().ToString(),
            OrderStatus.IntakeComplete);

        await SeedTimelineEventAsync(factory, allowedOrder, "intake.submission_received");
        await SeedTimelineEventAsync(factory, deniedOrder, "intake.submission_received");

        var allowedResponse = await client.GetAsync($"/api/orders/{allowedOrder}/timeline");
        Assert.That(allowedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var deniedResponse = await client.GetAsync($"/api/orders/{deniedOrder}/timeline");
        Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    private static void SetDefaultAuth(HttpClient client, string subject, string email)
    {
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", subject);
        client.DefaultRequestHeaders.Add("X-Auth-Email", email);
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

    private static async Task SeedOrderSummaryAsync(
        HolmesWebApplicationFactory factory,
        string orderId,
        string customerId,
        string subjectId,
        OrderStatus status
    )
    {
        using var scope = factory.Services.CreateScope();
        var workflowDb = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        workflowDb.OrderSummaries.Add(new OrderSummaryProjectionDb
        {
            OrderId = orderId,
            CustomerId = customerId,
            SubjectId = subjectId,
            PolicySnapshotId = "policy-v1",
            PackageCode = null,
            Status = status.ToString(),
            LastStatusReason = "seeded",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            LastUpdatedAt = DateTimeOffset.UtcNow,
            ReadyForRoutingAt = null,
            ClosedAt = null,
            CanceledAt = null
        });
        await workflowDb.SaveChangesAsync();
    }

    private static async Task SeedTimelineEventAsync(
        HolmesWebApplicationFactory factory,
        string orderId,
        string eventType
    )
    {
        using var scope = factory.Services.CreateScope();
        var workflowDb = scope.ServiceProvider.GetRequiredService<WorkflowDbContext>();
        workflowDb.OrderTimelineEvents.Add(new OrderTimelineEventProjectionDb
        {
            EventId = Ulid.NewUlid().ToString(),
            OrderId = orderId,
            EventType = eventType,
            Description = $"Seeded {eventType}",
            Source = "tests",
            OccurredAt = DateTimeOffset.UtcNow,
            RecordedAt = DateTimeOffset.UtcNow,
            MetadataJson = null
        });
        await workflowDb.SaveChangesAsync();
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

        customersDb.CustomerDirectory.Add(new CustomerDirectoryDb
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

        var directory = await customersDb.CustomerDirectory.SingleAsync(d => d.CustomerId == customerId);
        directory.AdminCount += 1;
        await customersDb.SaveChangesAsync();
    }
}
