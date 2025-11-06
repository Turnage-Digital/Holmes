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
        client.DefaultRequestHeaders.Add("X-Test-Subject", "user-no-admin");
        client.DefaultRequestHeaders.Add("X-Test-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Test-Email", "user@holmes.dev");

        await client.GetAsync("/users/me");

        var response = await client.PostAsJsonAsync("/customers", new CreateCustomerRequest("Acme"));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task Admin_Can_Create_And_Assign_Customer()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Subject", "admin-user");
        client.DefaultRequestHeaders.Add("X-Test-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Test-Email", "admin@holmes.dev");

        var adminId = await PromoteCurrentUserToAdminAsync(factory);

        var createResponse = await client.PostAsJsonAsync("/customers", new CreateCustomerRequest("Acme Industries"));
        if (createResponse.StatusCode != HttpStatusCode.Created)
        {
            TestContext.WriteLine(await createResponse.Content.ReadAsStringAsync());
        }
        Assert.That(createResponse.StatusCode, Is.EqualTo(HttpStatusCode.Created));

        var summary = await createResponse.Content.ReadFromJsonAsync<CustomerSummaryResponse>(JsonOptions);
        Assert.That(summary, Is.Not.Null);
        var customerId = summary!.CustomerId;

        var targetUserId = await CreateUserAsync(factory, "customer-admin", "cust.admin@holmes.dev");

        var assignResponse = await client.PostAsJsonAsync($"/customers/{customerId}/admins",
            new ModifyCustomerAdminRequest(targetUserId));
        Assert.That(assignResponse.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/customers/{customerId}/admins")
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
            DateTimeOffset.UtcNow));
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
            DateTimeOffset.UtcNow));

        await mediator.Send(new GrantUserRoleCommand(
            id,
            UserRole.Admin,
            null,
            id,
            DateTimeOffset.UtcNow));

        return id;
    }

    private sealed record CreateCustomerRequest(string Name);

    private sealed record ModifyCustomerAdminRequest(string UserId);

    private sealed record CustomerSummaryResponse(
        string CustomerId,
        string Name,
        CustomerStatus Status,
        DateTimeOffset CreatedAt,
        int AdminCount
    );
}
