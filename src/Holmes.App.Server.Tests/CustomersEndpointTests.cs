using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Domain;
using Holmes.Customers.Infrastructure.Sql;
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

        var summary = await createResponse.Content.ReadFromJsonAsync<CustomerSummaryResponse>(JsonOptions);
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
        var directory = await customersDb.CustomerDirectory.AsNoTracking()
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
            true));
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
            true));

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

    private sealed record CustomerSummaryResponse(
        string Id,
        string TenantId,
        string Name,
        CustomerStatus Status,
        string PolicySnapshotId,
        string? BillingEmail,
        DateTimeOffset CreatedAt,
        DateTimeOffset UpdatedAt,
        IReadOnlyCollection<CustomerContactResponse> Contacts
    );

    private sealed record CustomerContactResponse(
        string Id,
        string Name,
        string Email,
        string? Phone,
        string? Role
    );
}