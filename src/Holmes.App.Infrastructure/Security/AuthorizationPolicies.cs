namespace Holmes.App.Infrastructure.Security;

public static class AuthorizationPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireOps = "RequireOps";
    public const string RequireGlobalAdmin = "RequireGlobalAdmin";
}
