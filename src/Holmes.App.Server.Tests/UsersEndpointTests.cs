using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Abstractions.Dtos;
using Holmes.Users.Application.Commands;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class UsersEndpointTests
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task GetMe_Returns_Profile_ForAuthenticatedUser()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "user-01");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "user01@holmes.dev");
        client.DefaultRequestHeaders.Add("X-Auth-Name", "User One");

        await CreateUserAsync(factory, "user-01", "user01@holmes.dev");

        var response = await client.GetAsync("/api/users/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var payload = await response.Content.ReadFromJsonAsync<CurrentUserDto>(JsonOptions);
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Email, Is.EqualTo("user01@holmes.dev"));
        Assert.That(payload.Roles, Is.Empty);
    }

    [Test]
    public async Task GrantRole_Forbids_WhenCallerNotAdmin()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "non-admin");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "user@holmes.dev");

        await CreateUserAsync(factory, "non-admin", "user@holmes.dev");
        var targetId = await CreateUserAsync(factory, "target-user", "target@holmes.dev");

        var result = await client.PostAsJsonAsync($"/api/users/{targetId}/roles",
            new ModifyUserRoleRequest(UserRole.Admin, null));

        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GrantRole_Succeeds_For_Global_Admin()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "admin-user");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "admin@holmes.dev");
        client.DefaultRequestHeaders.Add("X-Auth-Name", "Admin User");

        var adminId = await PromoteCurrentUserToAdminAsync(factory);
        var targetId = await CreateUserAsync(factory, "target-admin", "target.admin@holmes.dev");

        var result = await client.PostAsJsonAsync($"/api/users/{targetId}/roles",
            new ModifyUserRoleRequest(UserRole.Admin, null));

        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var membership = await db.UserRoleMemberships.AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == targetId && r.Role == UserRole.Admin);
        Assert.That(membership, Is.Not.Null);
    }

    [Test]
    public async Task GetUsers_Returns_Forbidden_When_Not_Admin()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Roles", "Operations"); // lacks Admin
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "ops-user");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "ops@holmes.dev");

        await CreateUserAsync(factory, "ops-user", "ops@holmes.dev");

        var response = await client.GetAsync("/api/users");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
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

    private sealed record ModifyUserRoleRequest(UserRole Role, string? CustomerId);
}