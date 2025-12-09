using System.Security.Claims;
using Holmes.Core.Application.Abstractions;

namespace Holmes.App.Server.Infrastructure;

/// <summary>
/// HTTP-based tenant context that extracts tenant and actor from the current HTTP request claims.
/// </summary>
public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private const string TenantClaim = "tenant_id";
    private const string DefaultTenantId = "*";

    public string TenantId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return DefaultTenantId;
            }

            var tenantClaim = user.FindFirst(TenantClaim);
            return tenantClaim?.Value ?? DefaultTenantId;
        }
    }

    public string? ActorId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            // Try standard claims first, then common alternatives
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user.FindFirst("sub")?.Value;
        }
    }
}
