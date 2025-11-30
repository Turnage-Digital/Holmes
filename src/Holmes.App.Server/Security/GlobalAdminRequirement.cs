using Microsoft.AspNetCore.Authorization;

namespace Holmes.App.Server.Security;

public sealed class GlobalAdminRequirement : IAuthorizationRequirement;

public sealed class GlobalAdminAuthorizationHandler(ICurrentUserAccess currentUserAccess)
    : AuthorizationHandler<GlobalAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GlobalAdminRequirement requirement
    )
    {
        // If the user context cannot resolve, fail silently; caller will be forbidden.
        if (await currentUserAccess.IsGlobalAdminAsync(CancellationToken.None))
        {
            context.Succeed(requirement);
        }
    }
}