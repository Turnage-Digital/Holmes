using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Customers.Infrastructure.Sql.Entities;
using Holmes.Notifications.Application.Abstractions.Dtos;
using Holmes.Notifications.Domain;
using Holmes.Notifications.Infrastructure.Sql;
using Holmes.Notifications.Infrastructure.Sql.Entities;
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
public class NotificationsEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task GetByOrderId_Returns_Notifications_For_Order()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "notif-admin", "notif-admin@holmes.dev");

        await PromoteCurrentUserToAdminAsync(factory, "notif-admin", "notif-admin@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        await SeedCustomerAsync(factory, customerId, "tenant-notif");
        await SeedOrderSummaryAsync(factory, orderId, customerId);
        await SeedNotificationAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, DeliveryStatus.Delivered);
        await SeedNotificationAsync(factory, Ulid.NewUlid().ToString(), orderId, customerId, DeliveryStatus.Pending);

        var response = await client.GetAsync($"/api/notifications?orderId={orderId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var notifications =
            await response.Content.ReadFromJsonAsync<IReadOnlyList<NotificationSummaryDto>>(JsonOptions);
        Assert.That(notifications, Is.Not.Null);
        Assert.That(notifications!, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task GetByOrderId_Enforces_Customer_Access()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "notif-viewer", "notif-viewer@holmes.dev");

        var userId = await CreateUserAsync(factory, "notif-viewer", "notif-viewer@holmes.dev");
        var adminId = await PromoteCurrentUserToAdminAsync(factory, "notif-admin-seed", "notif-admin-seed@holmes.dev");

        var allowedCustomer = Ulid.NewUlid().ToString();
        var deniedCustomer = Ulid.NewUlid().ToString();
        var allowedOrder = Ulid.NewUlid().ToString();
        var deniedOrder = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, allowedCustomer, "tenant-allowed");
        await SeedCustomerAsync(factory, deniedCustomer, "tenant-denied");
        await AssignCustomerAdminAsync(factory, allowedCustomer, userId, adminId);

        await SeedOrderSummaryAsync(factory, allowedOrder, allowedCustomer);
        await SeedOrderSummaryAsync(factory, deniedOrder, deniedCustomer);
        await SeedNotificationAsync(factory, Ulid.NewUlid().ToString(), allowedOrder, allowedCustomer,
            DeliveryStatus.Delivered);
        await SeedNotificationAsync(factory, Ulid.NewUlid().ToString(), deniedOrder, deniedCustomer,
            DeliveryStatus.Delivered);

        var allowedResponse = await client.GetAsync($"/api/notifications?orderId={allowedOrder}");
        Assert.That(allowedResponse.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var deniedResponse = await client.GetAsync($"/api/notifications?orderId={deniedOrder}");
        Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GetByOrderId_Returns_BadRequest_When_OrderId_Missing()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "notif-admin", "notif-admin@holmes.dev");

        await PromoteCurrentUserToAdminAsync(factory, "notif-admin", "notif-admin@holmes.dev");

        var response = await client.GetAsync("/api/notifications");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetByOrderId_Returns_BadRequest_For_Invalid_OrderId()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "notif-admin", "notif-admin@holmes.dev");

        await PromoteCurrentUserToAdminAsync(factory, "notif-admin", "notif-admin@holmes.dev");

        var response = await client.GetAsync("/api/notifications?orderId=not-a-ulid");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    [Test]
    public async Task GetByOrderId_Returns_NotFound_For_NonExistent_Order()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "notif-admin", "notif-admin@holmes.dev");

        await PromoteCurrentUserToAdminAsync(factory, "notif-admin", "notif-admin@holmes.dev");

        var nonExistentOrderId = Ulid.NewUlid().ToString();
        var response = await client.GetAsync($"/api/notifications?orderId={nonExistentOrderId}");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Retry_Retries_Failed_Notification()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "notif-ops-retry", "notif-ops-retry@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "notif-ops-retry", "notif-ops-retry@holmes.dev");

        var customerId = Ulid.NewUlid().ToString();
        var orderId = Ulid.NewUlid().ToString();
        var notificationId = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, customerId, "tenant-retry");
        await SeedOrderSummaryAsync(factory, orderId, customerId);
        await SeedNotificationAsync(factory, notificationId, orderId, customerId, DeliveryStatus.Failed);

        var response = await client.PostAsync($"/api/notifications/{notificationId}/retry", null);

        if (response.StatusCode != HttpStatusCode.NoContent)
        {
            var body = await response.Content.ReadAsStringAsync();
            TestContext.WriteLine($"Response: {response.StatusCode} - {body}");
        }

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        // Verify notification was processed (status should change from Failed)
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();
        var notification = await db.NotificationRequests.FirstOrDefaultAsync(n => n.Id == notificationId);
        Assert.That(notification, Is.Not.Null);
        // After retry with the LoggingEmailProvider, it should be Delivered
        Assert.That(notification!.Status, Is.EqualTo((int)DeliveryStatus.Delivered));
    }

    [Test]
    public async Task Retry_Enforces_Customer_Access()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "notif-ops-limited", "notif-ops-limited@holmes.dev", "Operations");

        var userId = await CreateUserAsync(factory, "notif-ops-limited", "notif-ops-limited@holmes.dev");
        var adminId =
            await PromoteCurrentUserToAdminAsync(factory, "notif-admin-retry", "notif-admin-retry@holmes.dev");

        var allowedCustomer = Ulid.NewUlid().ToString();
        var deniedCustomer = Ulid.NewUlid().ToString();
        var allowedOrder = Ulid.NewUlid().ToString();
        var deniedOrder = Ulid.NewUlid().ToString();
        var allowedNotification = Ulid.NewUlid().ToString();
        var deniedNotification = Ulid.NewUlid().ToString();

        await SeedCustomerAsync(factory, allowedCustomer, "tenant-allowed-retry");
        await SeedCustomerAsync(factory, deniedCustomer, "tenant-denied-retry");
        await AssignCustomerAdminAsync(factory, allowedCustomer, userId, adminId);

        await SeedOrderSummaryAsync(factory, allowedOrder, allowedCustomer);
        await SeedOrderSummaryAsync(factory, deniedOrder, deniedCustomer);
        await SeedNotificationAsync(factory, allowedNotification, allowedOrder, allowedCustomer, DeliveryStatus.Failed);
        await SeedNotificationAsync(factory, deniedNotification, deniedOrder, deniedCustomer, DeliveryStatus.Failed);

        var allowedResponse = await client.PostAsync($"/api/notifications/{allowedNotification}/retry", null);
        Assert.That(allowedResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var deniedResponse = await client.PostAsync($"/api/notifications/{deniedNotification}/retry", null);
        Assert.That(deniedResponse.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Retry_Returns_NotFound_For_NonExistent_Notification()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "notif-ops-notfound", "notif-ops-notfound@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "notif-ops-notfound", "notif-ops-notfound@holmes.dev");

        var nonExistentNotificationId = Ulid.NewUlid().ToString();
        var response = await client.PostAsync($"/api/notifications/{nonExistentNotificationId}/retry", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));
    }

    [Test]
    public async Task Retry_Returns_BadRequest_For_Invalid_NotificationId()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        SetDefaultAuth(client, "notif-ops-invalid", "notif-ops-invalid@holmes.dev", "Operations");

        await PromoteCurrentUserToAdminAsync(factory, "notif-ops-invalid", "notif-ops-invalid@holmes.dev");

        var response = await client.PostAsync("/api/notifications/not-a-ulid/retry", null);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
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

    private static async Task SeedNotificationAsync(
        HolmesWebApplicationFactory factory,
        string notificationId,
        string orderId,
        string customerId,
        DeliveryStatus status
    )
    {
        using var scope = factory.Services.CreateScope();
        var notificationsDb = scope.ServiceProvider.GetRequiredService<NotificationsDbContext>();

        var now = DateTime.UtcNow;
        var notification = new NotificationRequestDb
        {
            Id = notificationId,
            CustomerId = customerId,
            OrderId = orderId,
            SubjectId = null,
            TriggerType = (int)NotificationTriggerType.OrderStateChanged,
            Channel = (int)NotificationChannel.Email,
            RecipientAddress = "test@example.com",
            RecipientDisplayName = "Test User",
            RecipientMetadataJson = "{}",
            ContentSubject = "Test Notification",
            ContentBody = "This is a test notification body.",
            ContentTemplateId = null,
            ContentTemplateDataJson = "{}",
            ScheduleJson = "{\"Type\":0}",
            Priority = 0,
            Status = (int)status,
            IsAdverseAction = false,
            CreatedAt = now.AddHours(-1),
            ScheduledFor = null,
            ProcessedAt = status == DeliveryStatus.Delivered ? now : null,
            DeliveredAt = status == DeliveryStatus.Delivered ? now : null,
            CorrelationId = null
        };

        notificationsDb.NotificationRequests.Add(notification);
        await notificationsDb.SaveChangesAsync();
    }
}