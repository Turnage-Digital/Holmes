using System.Security.Claims;
using Holmes.Core.Contracts;

namespace Holmes.App.Server;

/// <summary>
///     HTTP-based tenant context that extracts tenant and actor from the current HTTP request claims.
/// </summary>
public sealed class HttpTenantContext(IHttpContextAccessor httpContextAccessor) : ITenantContext
{
    private const string CustomerClaim = "customer_id";
    private const string ActorClaim = "holmes_user_id";
    private const string DefaultCustomerId = "*";

    public string? CustomerId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            var customerClaim = user.FindFirst(CustomerClaim);
            if (!string.IsNullOrWhiteSpace(customerClaim?.Value))
            {
                return customerClaim.Value;
            }

            return DefaultCustomerId;
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

            var actorClaim = user.FindFirst(ActorClaim);
            if (!string.IsNullOrWhiteSpace(actorClaim?.Value))
            {
                return actorClaim.Value;
            }

            // Try standard claims first, then common alternatives
            return user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? user.FindFirst("sub")?.Value;
        }
    }
}