using System.Security.Claims;

namespace Holmes.App.Infrastructure.Security;

public interface IUserContext
{
    ClaimsPrincipal Principal { get; }

    string Issuer { get; }

    string Subject { get; }

    string Email { get; }

    string? DisplayName { get; }

    string? AuthenticationMethod { get; }
}
