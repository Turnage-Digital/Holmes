using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Holmes.Core.Domain.ValueObjects;
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
        client.DefaultRequestHeaders.Add("X-Test-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Test-Subject", "user-01");
        client.DefaultRequestHeaders.Add("X-Test-Email", "user01@holmes.dev");
        client.DefaultRequestHeaders.Add("X-Test-Name", "User One");

        var response = await client.GetAsync("/users/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));

        var payload = await response.Content.ReadFromJsonAsync<UserResponse>(JsonOptions);
        Assert.That(payload, Is.Not.Null);
        Assert.That(payload!.Email, Is.EqualTo("user01@holmes.dev"));
        Assert.That(payload.Roles, Is.Empty);
    }

    [Test]
    public async Task GrantRole_Forbids_WhenCallerNotAdmin()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Test-Subject", "non-admin");
        client.DefaultRequestHeaders.Add("X-Test-Email", "user@holmes.dev");

        await client.GetAsync("/users/me");

        var targetId = await CreateUserAsync(factory, "target-user", "target@holmes.dev");

        var result = await client.PostAsJsonAsync($"/users/{targetId}/roles",
            new ModifyUserRoleRequest(UserRole.CustomerAdmin, null));

        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.Forbidden));
    }

    [Test]
    public async Task GrantRole_Succeeds_For_Global_Admin()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Test-Subject", "admin-user");
        client.DefaultRequestHeaders.Add("X-Test-Email", "admin@holmes.dev");
        client.DefaultRequestHeaders.Add("X-Test-Name", "Admin User");

        var adminId = await PromoteCurrentUserToAdminAsync(factory);

        var targetId = await CreateUserAsync(factory, "target-admin", "target.admin@holmes.dev");

        var result = await client.PostAsJsonAsync($"/users/{targetId}/roles",
            new ModifyUserRoleRequest(UserRole.CustomerAdmin, null));

        Assert.That(result.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UsersDbContext>();
        var membership = await db.UserRoleMemberships.AsNoTracking()
            .SingleOrDefaultAsync(r => r.UserId == targetId && r.Role == UserRole.CustomerAdmin);
        Assert.That(membership, Is.Not.Null);

        var revokeRequest = new HttpRequestMessage(HttpMethod.Delete, $"/users/{targetId}/roles")
        {
            Content = JsonContent.Create(new ModifyUserRoleRequest(UserRole.CustomerAdmin, null))
        };
        var revoke = await client.SendAsync(revokeRequest);
        Assert.That(revoke.StatusCode, Is.EqualTo(HttpStatusCode.NoContent));
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

    private sealed record ModifyUserRoleRequest(UserRole Role, string? CustomerId);

    private sealed record UserRoleResponse(UserRole Role, string? CustomerId);

    private sealed record UserResponse(
        string UserId,
        string Email,
        string? DisplayName,
        string Issuer,
        string Subject,
        UserStatus Status,
        DateTimeOffset LastAuthenticatedAt,
        IReadOnlyCollection<UserRoleResponse> Roles
    );
}