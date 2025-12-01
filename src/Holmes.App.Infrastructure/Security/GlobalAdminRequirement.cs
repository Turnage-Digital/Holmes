using Microsoft.AspNetCore.Authorization;

namespace Holmes.App.Infrastructure.Security;

public sealed class GlobalAdminRequirement : IAuthorizationRequirement;

public sealed class GlobalAdminAuthorizationHandler(ICurrentUserAccess currentUserAccess)
    : AuthorizationHandler<GlobalAdminRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        GlobalAdminRequirement requirement
    )
    {
        if (await currentUserAccess.IsGlobalAdminAsync(CancellationToken.None))
        {
            context.Succeed(requirement);
        }
    }
}