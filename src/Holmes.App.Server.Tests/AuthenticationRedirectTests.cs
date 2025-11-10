using System.Net;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class AuthenticationRedirectTests
{
    [Test]
    public async Task Html_Request_Without_Session_Redirects_To_AuthOptions()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));

        var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));

        var location = response.Headers.Location;
        Assert.That(location, Is.Not.Null, "Expected a redirect to /auth/options.");

        var actual = location!.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
        Assert.That(actual, Is.EqualTo("/auth/options?returnUrl=%2Fusers"));
    }
}