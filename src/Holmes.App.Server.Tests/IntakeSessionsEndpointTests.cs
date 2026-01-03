using System.Net;
using System.Net.Http.Json;
using Holmes.Core.Domain;
using Holmes.Users.Application.Commands;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Holmes.App.Server.Tests;

[TestFixture]
public class IntakeSessionsEndpointTests
{
    [Test]
    public async Task StartSession_Returns_BadRequest_For_Invalid_Id()
    {
        await using var factory = new HolmesWebApplicationFactory();
        var client = factory.CreateClient();
        client.DefaultRequestHeaders.Add("X-Auth-Subject", "intake-tester");
        client.DefaultRequestHeaders.Add("X-Auth-Issuer", "https://issuer.holmes.test");
        client.DefaultRequestHeaders.Add("X-Auth-Email", "intake@test.holmes");

        await EnsureTestUserAsync(factory);

        var payload = new StartSessionRequest("resume-token-1");
        var response = await client.PostAsJsonAsync("/api/intake/sessions/not-a-ulid/start", payload);

        if (response.StatusCode != HttpStatusCode.BadRequest)
        {
            TestContext.WriteLine($"Status: {response.StatusCode}");
            TestContext.WriteLine(await response.Content.ReadAsStringAsync());
        }

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.BadRequest));
    }

    private static async Task EnsureTestUserAsync(HolmesWebApplicationFactory factory)
    {
        using var scope = factory.Services.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Send(new RegisterExternalUserCommand(
            "https://issuer.holmes.test",
            "intake-tester",
            "intake@test.holmes",
            "Intake Tester",
            "pwd",
            DateTimeOffset.UtcNow,
            true)
        {
            UserId = SystemActors.System
        });
    }

    private sealed record StartSessionRequest(string ResumeToken);
}