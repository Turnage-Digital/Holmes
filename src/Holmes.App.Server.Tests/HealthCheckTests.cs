using Microsoft.AspNetCore.Mvc.Testing;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class HealthCheckTests
{
    [Test]
    public async Task Health_Endpoint_Returns_Ok()
    {
        await using var factory = new WebApplicationFactory<Program>();
        var client = factory.CreateClient();

        var response = await client.GetAsync("/health");

        Assert.That(response.IsSuccessStatusCode, Is.True);
    }
}
