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
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

        using var request = new HttpRequestMessage(HttpMethod.Get, "/users");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
        request.Headers.Add("X-Auth-Skip", "1");

        var response = await client.SendAsync(request);

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.Redirect));

        var location = response.Headers.Location;
        Assert.That(location, Is.Not.Null, "Expected a redirect to /auth/options.");

        var actual = location!.IsAbsoluteUri ? location.PathAndQuery : location.OriginalString;
        Assert.That(actual, Is.EqualTo("/auth/options?returnUrl=%2Fusers"));
    }

    [Test]
    public async Task AuthOptions_Sanitizes_ReturnUrl()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            AllowAutoRedirect = false
        });

        var response = await client.GetAsync("/auth/options?returnUrl=https://evil.example/phish");

        TestContext.WriteLine($"Status: {response.StatusCode}");
        if (response.Headers.Location is { } location)
        {
            TestContext.WriteLine($"Location: {location}");
        }

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
        var html = await response.Content.ReadAsStringAsync();
        Assert.That(html, Does.Contain("/auth/login?returnUrl=%2F"));
    }
}
