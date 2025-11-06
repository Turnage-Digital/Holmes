using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Holmes.App.Server.Tests;

internal sealed class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
#pragma warning disable CS0618
    public TestAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock
    )
        : base(options, logger, encoder, clock)
    {
    }
#pragma warning restore CS0618

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var issuer = Request.Headers["X-Test-Issuer"].FirstOrDefault() ?? "https://issuer.testing";
        var subject = Request.Headers["X-Test-Subject"].FirstOrDefault() ?? "subject-testing";
        var email = Request.Headers["X-Test-Email"].FirstOrDefault() ?? "user@test.dev";
        var name = Request.Headers["X-Test-Name"].FirstOrDefault();
        var amr = Request.Headers["X-Test-Amr"].FirstOrDefault() ?? "pwd";

        var claims = new List<Claim>
        {
            new("iss", issuer),
            new("sub", subject),
            new(ClaimTypes.Email, email),
            new("amr", amr)
        };

        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}