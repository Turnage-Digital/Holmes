using System.Net;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Exceptions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class UserInitializationTests
{
    [Test]
    public async Task Uninvited_User_Is_Redirected_To_AccessDenied()
    {
        await using var factory = new HolmesWebApplicationFactory();
        await using var customized = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                services.AddScoped<ICurrentUserInitializer, ThrowingInitializer>();
            });
        });

        var client = customized.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/api/users/me");

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));
        var location = response.Headers.Location;
        Assert.That(location, Is.Not.Null);
        var actual = location!.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
        Assert.That(actual, Is.EqualTo("/auth/access-denied?reason=uninvited"));
    }

    private sealed class ThrowingInitializer : ICurrentUserInitializer
    {
        public Task<UlidId> EnsureCurrentUserIdAsync(CancellationToken cancellationToken)
        {
            throw new UserInvitationRequiredException("uninvited@holmes.dev", "https://issuer", "subject-1");
        }
    }
}