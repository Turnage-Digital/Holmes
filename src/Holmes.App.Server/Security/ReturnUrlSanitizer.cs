namespace Holmes.App.Server.Security;

internal static class ReturnUrlSanitizer
{
    public static string Sanitize(string? returnUrl, HttpRequest request)
    {
        if (string.IsNullOrWhiteSpace(returnUrl))
        {
            return "/";
        }

        if (Uri.TryCreate(returnUrl, UriKind.RelativeOrAbsolute, out var candidate) &&
            candidate.IsAbsoluteUri)
        {
            var requestHost = request.Host.HasValue ? request.Host.Host : string.Empty;
            if (!string.Equals(candidate.Host, requestHost, StringComparison.OrdinalIgnoreCase))
            {
                return "/";
            }

            return candidate.PathAndQuery;
        }

        return returnUrl.StartsWith('/') ? returnUrl : "/";
    }
}