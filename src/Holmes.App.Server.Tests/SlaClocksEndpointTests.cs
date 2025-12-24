using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.App.Server.Controllers;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.SlaClocks.Application.Abstractions.Dtos;
using Holmes.SlaClocks.Domain;
using Holmes.SlaClocks.Infrastructure.Sql;
using Holmes.SlaClocks.Infrastructure.Sql.Entities;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Orders.Domain;
using Holmes.Orders.Infrastructure.Sql;
using Holmes.Orders.Infrastructure.Sql.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class SlaClocksEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(), new UlidIdJsonConverter() }
    };

    [Test]
    public async Task GetByOrderId_Returns_Clocks_For_Order()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-admin", "admin@holmes.dev");

        await PromoteCurrentUserToAdminAsync(factory, "clocks-admin", "admin@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerId, "tenant-clocks");
        await SeedOrderSummaryAsync(factory, orderId, customerId);
        await SeedSlaClockAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, ClockKind.Intake,
            ClockState.Running);
        await SeedSlaClockAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, ClockKind.Overall,
            ClockState.Running);

        var response = await client.GetAsync($"/api/clocks/sla?orderId={orderId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var clocks = await response.Content.ReadFromJsonAsync<IReadOnlyList<SlaClockDto>>(JsonOptions);
        Assert.That(clocks, Is.Not.Null);
        Assert.That(clocks!, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetByOrderId_Enforces_Customer_Access()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-viewer", "viewer@holmes.dev");

        var userId = await CreateUserAsync(factory, "clocks-viewer", "viewer@holmes.dev");
        var adminId = await PromoteCurrentUserToAdminAsync(factory, "clocks-admin-seed", "admin@holmes.dev");

        var allowedCustomer = Ulid.NewUlid().ToString();
        var deniedCustomer = Ulid.NewUlid().ToString();
        var allowedOrder = Ulid.NewUlid().ToString();
        var deniedOrder = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, allowedCustomer, "tenant-allowed");
        await SeedCustomerAsync(factory, deniedCustomer, "tenant-denied");
        await AssignCustomerAdminAsync(factory, allowedCustomer, userId, adminId);

        await SeedOrderSummaryAsync(factory, allowedOrder, allowedCustomer);
        await SeedOrderSummaryAsync(factory, deniedOrder, deniedCustomer);
        await SeedSlaClockAsync(factory, Ulid.NewUlid().ToString(), allowedOrder, allowedCustomer, ClockKind.Intake,
            ClockState.Running);
        await SeedSlaClockAsync(factory, Ulid.NewUlid().ToString(), deniedOrder, deniedCustomer, ClockKind.Intake,
            ClockState.Running);

        var allowedResponse = await client.GetAsync($"/api/clocks/sla?orderId={allowedOrder}");
        Assert.That(allowedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var deniedResponse = await client.GetAsync($"/api/clocks/sla?orderId={deniedOrder}");
        Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetByOrderId_Returns_BadRequest_When_OrderId_Missing()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-admin", "admin@holmes.dev");

        await PromoteCurrentUserToAdminAsync(factory, "clocks-admin", "admin@holmes.dev");

        var response = await client.GetAsync("/api/clocks/sla");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetByOrderId_Returns_BadRequest_For_Invalid_OrderId()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-admin", "admin@holmes.dev");

        await PromoteCurrentUserToAdminAsync(factory, "clocks-admin", "admin@holmes.dev");

        var response = await client.GetAsync("/api/clocks/sla?orderId=not-a-ulid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetByOrderId_Returns_NotFound_For_NonExistent_Order()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-admin", "admin@holmes.dev");

        await PromoteCurrentUserToAdminAsync(factory, "clocks-admin", "admin@holmes.dev");

        var nonExistentOrderId = Ulid.NewUlid().ToString();
        var response = await client.GetAsync($"/api/clocks/sla?orderId={nonExistentOrderId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Pause_Pauses_Running_Clock()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-ops-pause", "clocks-ops-pause@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "clocks-ops-pause", "clocks-ops-pause@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        var clockId = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, customerId, "tenant-pause");
        await SeedOrderSummaryAsync(factory, orderId, customerId);
        await SeedSlaClockAsync(factory, clockId, orderId, customerId, ClockKind.Intake, ClockState.Running);

        var request = new SlaClocksController.PauseClockRequest("Order is blocked pending clarification");
        var response = await client.PostAsJsonAsync($"/api/clocks/sla/{clockId}/pause", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify clock is now paused
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SlaClockDbContext>();
        var clock = await db.SlaClocks.FirstOrDefaultAsync(c => c.Id == clockId);
        Assert.That(clock, Is.Not.Null);
        Assert.That(clock!.State, Is.EqualTo((int)ClockState.Paused));
        Assert.That(clock.PauseReason, Is.EqualTo("Order is blocked pending clarification"));
    }

    [Test]
    public async Task Pause_Returns_BadRequest_When_Reason_Missing()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-ops-bad", "clocks-ops-bad@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "clocks-ops-bad", "clocks-ops-bad@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        var clockId = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, customerId, "tenant-pause-bad");
        await SeedOrderSummaryAsync(factory, orderId, customerId);
        await SeedSlaClockAsync(factory, clockId, orderId, customerId, ClockKind.Intake, ClockState.Running);

        var request = new SlaClocksController.PauseClockRequest("");
        var response = await client.PostAsJsonAsync($"/api/clocks/sla/{clockId}/pause", request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task Pause_Enforces_Customer_Access()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-ops-limited", "ops-limited@holmes.dev", "Operations");

        var userId = await CreateUserAsync(factory, "clocks-ops-limited", "ops-limited@holmes.dev");
        var adminId = await PromoteCurrentUserToAdminAsync(factory, "clocks-admin-pause", "admin@holmes.dev");

        var allowedCustomer = Ulid.NewUlid().ToString();
        var deniedCustomer = Ulid.NewUlid().ToString();
        var allowedOrder = Ulid.NewUlid().ToString();
        var deniedOrder = Ulid.NewUlid().ToString();
        var allowedClock = Ulid.NewUlid().ToString();
        var deniedClock = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, allowedCustomer, "tenant-allowed-pause");
        await SeedCustomerAsync(factory, deniedCustomer, "tenant-denied-pause");
        await AssignCustomerAdminAsync(factory, allowedCustomer, userId, adminId);

        await SeedOrderSummaryAsync(factory, allowedOrder, allowedCustomer);
        await SeedOrderSummaryAsync(factory, deniedOrder, deniedCustomer);
        await SeedSlaClockAsync(factory, allowedClock, allowedOrder, allowedCustomer, ClockKind.Intake,
            ClockState.Running);
        await SeedSlaClockAsync(factory, deniedClock, deniedOrder, deniedCustomer, ClockKind.Intake,
            ClockState.Running);

        var request = new SlaClocksController.PauseClockRequest("Test pause");

        var allowedResponse = await client.PostAsJsonAsync($"/api/clocks/sla/{allowedClock}/pause", request);
        Assert.That(allowedResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var deniedResponse = await client.PostAsJsonAsync($"/api/clocks/sla/{deniedClock}/pause", request);
        Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Resume_Resumes_Paused_Clock()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-ops-resume", "ops-resume@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "clocks-ops-resume", "ops-resume@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        var clockId = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, customerId, "tenant-resume");
        await SeedOrderSummaryAsync(factory, orderId, customerId);
        await SeedSlaClockAsync(factory, clockId, orderId, customerId, ClockKind.Intake, ClockState.Paused,
            "Previously paused");

        var response = await client.PostAsync($"/api/clocks/sla/{clockId}/resume", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify clock is now running
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SlaClockDbContext>();
        var clock = await db.SlaClocks.FirstOrDefaultAsync(c => c.Id == clockId);
        Assert.That(clock, Is.Not.Null);
        Assert.That(clock!.State, Is.EqualTo((int)ClockState.Running));
    }

    [Test]
    public async Task Resume_Enforces_Customer_Access()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-ops-resume-limited", "ops-resume-limited@holmes.dev", "Operations");

        var userId = await CreateUserAsync(factory, "clocks-ops-resume-limited", "ops-resume-limited@holmes.dev");
        var adminId = await PromoteCurrentUserToAdminAsync(factory, "clocks-admin-resume", "admin-resume@holmes.dev");

        var allowedCustomer = Ulid.NewUlid().ToString();
        var deniedCustomer = Ulid.NewUlid().ToString();
        var allowedOrder = Ulid.NewUlid().ToString();
        var deniedOrder = Ulid.NewUlid().ToString();
        var allowedClock = Ulid.NewUlid().ToString();
        var deniedClock = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, allowedCustomer, "tenant-allowed-resume");
        await SeedCustomerAsync(factory, deniedCustomer, "tenant-denied-resume");
        await AssignCustomerAdminAsync(factory, allowedCustomer, userId, adminId);

        await SeedOrderSummaryAsync(factory, allowedOrder, allowedCustomer);
        await SeedOrderSummaryAsync(factory, deniedOrder, deniedCustomer);
        await SeedSlaClockAsync(factory, allowedClock, allowedOrder, allowedCustomer, ClockKind.Intake,
            ClockState.Paused, "Paused");
        await SeedSlaClockAsync(factory, deniedClock, deniedOrder, deniedCustomer, ClockKind.Intake, ClockState.Paused,
            "Paused");

        var allowedResponse = await client.PostAsync($"/api/clocks/sla/{allowedClock}/resume", null);
        Assert.That(allowedResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var deniedResponse = await client.PostAsync($"/api/clocks/sla/{deniedClock}/resume", null);
        Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Resume_Returns_NotFound_For_NonExistent_Clock()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "clocks-ops-notfound", "ops-notfound@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "clocks-ops-notfound", "ops-notfound@holmes.dev");

        var nonExistentClockId = Ulid.NewUlid().ToString();
        var response = await client.PostAsync($"/api/clocks/sla/{nonExistentClockId}/resume", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
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
        var workflowDb = scope.ServiceProvider.GetRequiredService<OrdersDbContext>();
        workflowDb.OrderSummaries.Add(new OrderSummaryProjectionDb
        {
            OrderId = orderId,
            CustomerId = customerId,
            SubjectId = Ulid.NewUlid().ToString(),
            PolicySnapshotId = "policy-v1",
            PackageCode = null,
            Status = OrderStatus.Invited.ToString(),
            LastStatusReason = "seeded",
            CreatedAt = DateTimeOffset.UtcNow.AddMinutes(-30),
            LastUpdatedAt = DateTimeOffset.UtcNow,
            ReadyForFulfillmentAt = null,
            ClosedAt = null,
            CanceledAt = null
        });
        await workflowDb.SaveChangesAsync();
    }

    private static async Task SeedSlaClockAsync(
        HolmesWebApplicationFactory factory,
        string clockId,
        string orderId,
        string customerId,
        ClockKind kind,
        ClockState state,
        string? pauseReason = null
    )
    {
        using var scope = factory.Services.CreateScope();
        var slaDb = scope.ServiceProvider.GetRequiredService<SlaClockDbContext>();

        var now = DateTime.UtcNow;
        var clock = new SlaClockDb
        {
            Id = clockId,
            OrderId = orderId,
            CustomerId = customerId,
            Kind = (int)kind,
            State = (int)state,
            StartedAt = now.AddHours(-1),
            DeadlineAt = now.AddDays(1),
            AtRiskThresholdAt = now.AddHours(18),
            AtRiskAt = state == ClockState.AtRisk ? now : null,
            BreachedAt = state == ClockState.Breached ? now : null,
            PausedAt = state == ClockState.Paused ? now : null,
            CompletedAt = state == ClockState.Completed ? now : null,
            PauseReason = pauseReason,
            AccumulatedPauseMs = 0,
            TargetBusinessDays = 1,
            AtRiskThresholdPercent = 0.80m
        };

        slaDb.SlaClocks.Add(clock);
        await slaDb.SaveChangesAsync();
    }
}