// Lightweight header-based auth used in development/testing (no OIDC authority configured).
// Keeps tooling/automation unblocked by allowing callers to supply identities via X-Auth-* headers.
using System.Collections.Generic;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Holmes.App.Server.Security;

public sealed class HeaderAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
#pragma warning disable CS0618
    public HeaderAuthenticationHandler(
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
        var issuer = GetHeader(HeaderAuthenticationDefaults.Headers.Issuer);
        var subject = GetHeader(HeaderAuthenticationDefaults.Headers.Subject);
        var email = GetHeader(HeaderAuthenticationDefaults.Headers.Email);

        if (issuer is null || subject is null || email is null)
        {
            var missing = new List<string>();
            if (issuer is null)
            {
                missing.Add(HeaderAuthenticationDefaults.Headers.Issuer);
            }

            if (subject is null)
            {
                missing.Add(HeaderAuthenticationDefaults.Headers.Subject);
            }

            if (email is null)
            {
                missing.Add(HeaderAuthenticationDefaults.Headers.Email);
            }

            return Task.FromResult(
                AuthenticateResult.Fail($"Missing authentication headers: {string.Join(", ", missing)}"));
        }

        var claims = new List<Claim>
        {
            new("iss", issuer),
            new("sub", subject),
            new(ClaimTypes.Email, email)
        };

        var name = GetHeader(HeaderAuthenticationDefaults.Headers.Name);
        if (!string.IsNullOrWhiteSpace(name))
        {
            claims.Add(new Claim(ClaimTypes.Name, name));
        }

        var amr = GetHeader(HeaderAuthenticationDefaults.Headers.AuthenticationMethod) ?? "pwd";
        claims.Add(new Claim("amr", amr));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);
        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    }

    private string? GetHeader(string name)
    {
        if (Request.Headers.TryGetValue(name, out var values))
        {
            var value = values.FirstOrDefault();
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return null;
    }
}
