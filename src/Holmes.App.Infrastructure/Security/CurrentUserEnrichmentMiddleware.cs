using System.Security.Claims;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Infrastructure.Sql;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Infrastructure.Security;

public sealed class CurrentUserEnrichmentMiddleware(RequestDelegate next)
{
    private const string UserIdClaimType = "holmes_user_id";

    public async Task InvokeAsync(
        HttpContext context,
        ICurrentUserInitializer currentUserInitializer,
        UsersDbContext usersDbContext
    )
    {
        var principal = context.User;
        if (principal?.Identity?.IsAuthenticated != true)
        {
            await next(context);
            return;
        }

        if (principal.Identity is not ClaimsIdentity identity)
        {
            await next(context);
            return;
        }

        var userId = await EnsureUserIdClaimAsync(identity, currentUserInitializer, context.RequestAborted);
        await EnrichRoleClaimsAsync(identity, usersDbContext, userId, context.RequestAborted);

        await next(context);
    }

    private static async Task<UlidId> EnsureUserIdClaimAsync(
        ClaimsIdentity identity,
        ICurrentUserInitializer currentUserInitializer,
        CancellationToken cancellationToken
    )
    {
        var existing = identity.FindFirst(UserIdClaimType)?.Value;
        if (Ulid.TryParse(existing, out var parsed))
        {
            return UlidId.FromUlid(parsed);
        }

        var userId = await currentUserInitializer.EnsureCurrentUserIdAsync(cancellationToken);
        identity.AddClaim(new Claim(UserIdClaimType, userId.ToString()));
        return userId;
    }

    private static async Task EnrichRoleClaimsAsync(
        ClaimsIdentity identity,
        UsersDbContext usersDbContext,
        UlidId userId,
        CancellationToken cancellationToken
    )
    {
        var roleClaimType = identity.RoleClaimType ?? ClaimTypes.Role;
        var existingRoles = identity.FindAll(roleClaimType)
            .Select(c => c.Value)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var roles = await usersDbContext.UserRoleMemberships
            .AsNoTracking()
            .Where(x => x.UserId == userId.ToString() && x.CustomerId == null)
            .Select(x => x.Role.ToString())
            .Distinct()
            .ToListAsync(cancellationToken);

        foreach (var role in roles)
        {
            if (existingRoles.Add(role))
            {
                identity.AddClaim(new Claim(roleClaimType, role));
            }
        }
    }
}
