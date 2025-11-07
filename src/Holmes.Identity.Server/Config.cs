using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Holmes.Identity.Server;

internal static class Config
{
    public static IEnumerable<IdentityResource> IdentityResources =>
        new List<IdentityResource>
        {
            new IdentityResources.OpenId(),
            new IdentityResources.Profile(),
            new("email", ["email"])
        };

    public static IEnumerable<ApiScope> ApiScopes =>
        new List<ApiScope>
        {
            new("holmes.api", "Holmes API")
        };

    public static IEnumerable<Client> Clients =>
        new List<Client>
        {
            new()
            {
                ClientId = "holmes_app",
                ClientName = "Holmes App",
                ClientSecrets = { new Secret("dev-secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = false,
                AllowOfflineAccess = true,
                RedirectUris =
                {
                    "https://localhost:5001/signin-oidc"
                },
                PostLogoutRedirectUris =
                {
                    "https://localhost:5001/signout-callback-oidc"
                },
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    "email"
                }
            }
        };

    public static IReadOnlyList<DevUser> DevUsers =>
        new List<DevUser>
        {
            new("1001", "admin", "password", "Dev Admin", "admin@holmes.dev", "Admin"),
            new("1002", "ops", "password", "Ops User", "ops@holmes.dev", "Ops")
        };
}

internal sealed record DevUser(
    string SubjectId,
    string Username,
    string Password,
    string DisplayName,
    string Email,
    string Role
);