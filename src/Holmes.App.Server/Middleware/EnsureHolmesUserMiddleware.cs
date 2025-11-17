using Holmes.App.Server.Security;
using Holmes.Core.Application;
using Holmes.Users.Application.Exceptions;
using Microsoft.AspNetCore.Authentication;

namespace Holmes.App.Server.Middleware;

internal sealed class EnsureHolmesUserMiddleware(
    ICurrentUserInitializer initializer,
    ILogger<EnsureHolmesUserMiddleware> logger
) : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (context.User.Identity?.IsAuthenticated is true && RequiresUserInitialization(context.Request))
        {
            try
            {
                await initializer.EnsureCurrentUserIdAsync(context.RequestAborted);
            }
            catch (UserInvitationRequiredException ex)
            {
                logger.LogWarning(ex, "Uninvited login attempt for {Email} ({Issuer}/{Subject})",
                    ex.Email, ex.Issuer, ex.Subject);
                if (!string.Equals(context.User.Identity?.AuthenticationType,
                        TestAuthenticationDefaults.Scheme, StringComparison.Ordinal))
                {
                    await context.SignOutAsync();
                }

                context.Response.Redirect("/auth/access-denied?reason=uninvited");
                return;
            }
        }

        await next(context);
    }

    private static bool RequiresUserInitialization(HttpRequest request)
    {
        var path = request.Path;
        if (!HttpMethods.IsGet(request.Method) && !HttpMethods.IsPost(request.Method))
        {
            return true;
        }

        if (path.StartsWithSegments("/signin-oidc", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/signout-callback-oidc", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/access-denied", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/login", StringComparison.OrdinalIgnoreCase) ||
            path.StartsWithSegments("/auth/options", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }
}