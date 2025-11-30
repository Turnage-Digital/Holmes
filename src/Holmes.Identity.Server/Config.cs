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

    public static IEnumerable<ApiResource> ApiResources =>
        new List<ApiResource>
        {
            new("holmes.api", "Holmes API")
            {
                Scopes = { "holmes.api" },
                UserClaims = { "role", "email", "name" }
            }
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
                RequirePkce = true,
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
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    "holmes.api"
                }
            },
            new()
            {
                ClientId = "holmes_internal",
                ClientName = "Holmes Internal SPA (BFF)",
                ClientSecrets = { new Secret("dev-internal-secret".Sha256()) },
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                AllowOfflineAccess = true,
                RedirectUris =
                {
                    "https://localhost:5003/signin-oidc",
                    "https://localhost:3000/signin-oidc"
                },
                PostLogoutRedirectUris =
                {
                    "https://localhost:5003/signout-callback-oidc",
                    "https://localhost:3000/signout-callback-oidc"
                },
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    IdentityServerConstants.StandardScopes.Email,
                    IdentityServerConstants.StandardScopes.OfflineAccess,
                    "holmes.api"
                }
            }
        };
}