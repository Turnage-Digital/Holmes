using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Holmes.App.Server.Security;

public class HttpUserContext(IHttpContextAccessor accessor) 
    : IUserContext
{
    public ClaimsPrincipal Principal =>
        accessor.HttpContext?.User ?? throw new InvalidOperationException("No active HttpContext user.");

    public string Issuer =>
        Principal.FindFirstValue("iss") ??
        Principal.Claims.FirstOrDefault()?.Issuer ??
        throw new InvalidOperationException("Required claim 'iss' was not present.");

    public string Subject =>
        Principal.FindFirstValue("sub") ??
        Principal.FindFirstValue(JwtRegisteredClaimNames.Sub) ??
        Principal.FindFirstValue(ClaimTypes.NameIdentifier) ??
        throw new InvalidOperationException("Required claim 'sub' was not present.");

    public string Email =>
        Principal.FindFirstValue(ClaimTypes.Email) ??
        Principal.FindFirstValue(JwtRegisteredClaimNames.Email) ??
        Principal.FindFirstValue("emails") ??
        Principal.FindFirstValue("email") ??
        Principal.FindFirstValue("preferred_username") ??
        Principal.FindFirstValue(ClaimTypes.Upn) ??
        Principal.Identity?.Name ??
        throw new InvalidOperationException("Required claim 'email' was not present.");

    public string? DisplayName => Principal.FindFirstValue(ClaimTypes.Name) ?? Principal.Identity?.Name;

    public string? AuthenticationMethod =>
        Principal.FindFirstValue("amr") ??
        Principal.FindFirstValue(ClaimTypes.AuthenticationMethod);
}
