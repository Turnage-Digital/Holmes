using System.Security.Claims;

namespace Holmes.App.Server.Security;

public class HttpUserContext : IUserContext
{
    private readonly IHttpContextAccessor _accessor;

    public HttpUserContext(IHttpContextAccessor accessor)
    {
        _accessor = accessor;
    }

    public ClaimsPrincipal Principal =>
        _accessor.HttpContext?.User ?? throw new InvalidOperationException("No active HttpContext user.");

    public string Issuer => GetClaim("iss");

    public string Subject => GetClaim("sub");

    public string Email => GetClaim(ClaimTypes.Email);

    public string? DisplayName => Principal.FindFirstValue(ClaimTypes.Name);

    public string? AuthenticationMethod => Principal.FindFirstValue("amr");

    private string GetClaim(string type)
    {
        var value = Principal.FindFirstValue(type);
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException($"Required claim '{type}' was not present.");
        }

        return value;
    }
}