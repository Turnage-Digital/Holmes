namespace Holmes.App.Server.Middleware;

internal sealed class RedirectToAuthOptionsMiddleware : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        if (!(context.User.Identity?.IsAuthenticated ?? false) &&
            ShouldRedirectToAuthOptions(context.Request))
        {
            var returnUrl = GetRequestedUrl(context.Request);
            context.Response.Redirect($"/auth/options?returnUrl={Uri.EscapeDataString(returnUrl)}");
            return;
        }

        await next(context);
    }

    private static bool ShouldRedirectToAuthOptions(HttpRequest request)
    {
        if (!HttpMethods.IsGet(request.Method))
        {
            return false;
        }

        var path = request.Path;
        if (path.StartsWithSegments("/auth") ||
            path.StartsWithSegments("/signin-oidc") ||
            path.StartsWithSegments("/signout-callback-oidc") ||
            path.StartsWithSegments("/api") ||
            path.StartsWithSegments("/health") ||
            path.StartsWithSegments("/swagger") ||
            path.StartsWithSegments("/static") ||
            path.StartsWithSegments("/assets"))
        {
            return false;
        }

        if (path.HasValue && path.Value.Contains('.', StringComparison.Ordinal))
        {
            return false;
        }

        if (!request.Headers.TryGetValue("Accept", out var acceptHeader))
        {
            return false;
        }

        return acceptHeader.Any(value =>
            value is not null &&
            value.Contains("text/html", StringComparison.OrdinalIgnoreCase));
    }

    private static string GetRequestedUrl(HttpRequest request)
    {
        var path = request.Path.HasValue ? request.Path.Value : "/";
        var query = request.QueryString.HasValue ? request.QueryString.Value : string.Empty;
        return string.Concat(path, query);
    }
}