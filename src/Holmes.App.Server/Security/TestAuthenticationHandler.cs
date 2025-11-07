using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace Holmes.App.Server.Security;

public static class TestAuthenticationDefaults
{
    public const string Scheme = "Test";
}

public sealed class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private const string IssuerHeader = "X-Auth-Issuer";
    private const string SubjectHeader = "X-Auth-Subject";
    private const string EmailHeader = "X-Auth-Email";
    private const string NameHeader = "X-Auth-Name";
    private const string AmrHeader = "X-Auth-Amr";
#pragma warning disable CS0618
    public TestAuthenticationHandler(
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
        var subject = GetHeader(SubjectHeader) ?? "test-user";
        var issuer = GetHeader(IssuerHeader) ?? "https://holmes.test";
        var email = GetHeader(EmailHeader) ?? $"{subject}@holmes.local";
        var name = GetHeader(NameHeader) ?? subject;
        var amr = GetHeader(AmrHeader) ?? "test";

        var claims = new List<Claim>
        {
            new("iss", issuer),
            new("sub", subject),
            new(ClaimTypes.NameIdentifier, subject),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new("amr", amr)
        };

        var identity = new ClaimsIdentity(claims, TestAuthenticationDefaults.Scheme);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, TestAuthenticationDefaults.Scheme);
        return Task.FromResult(AuthenticateResult.Success(ticket));
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