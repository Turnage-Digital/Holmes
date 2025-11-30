using System.Security.Claims;
using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Infrastructure.Sql;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql;
using Microsoft.EntityFrameworkCore;

namespace Holmes.App.Server.Security;

public sealed class CurrentUserAccess(
    IUserContext userContext,
    ICurrentUserInitializer currentUserInitializer,
    UsersDbContext usersDbContext,
    CustomersDbContext customersDbContext
) : ICurrentUserAccess
{
    private const string UserIdClaimType = "holmes_user_id";
    private Task<UserAccessSnapshot>? _snapshot;

    public async Task<UlidId> GetUserIdAsync(CancellationToken cancellationToken)
    {
        var snapshot = await EnsureSnapshotAsync(cancellationToken);
        return snapshot.UserId;
    }

    public async Task<bool> IsGlobalAdminAsync(CancellationToken cancellationToken)
    {
        var snapshot = await EnsureSnapshotAsync(cancellationToken);
        return snapshot.IsGlobalAdmin;
    }

    public async Task<IReadOnlyCollection<string>> GetAllowedCustomerIdsAsync(CancellationToken cancellationToken)
    {
        var snapshot = await EnsureSnapshotAsync(cancellationToken);
        return snapshot.AllowedCustomerIds;
    }

    public async Task<bool> HasCustomerAccessAsync(string customerId, CancellationToken cancellationToken)
    {
        var snapshot = await EnsureSnapshotAsync(cancellationToken);
        return snapshot.IsGlobalAdmin || snapshot.AllowedCustomerIds.Contains(customerId);
    }

    private Task<UserAccessSnapshot> EnsureSnapshotAsync(CancellationToken cancellationToken)
    {
        _snapshot ??= ResolveAsync(cancellationToken);
        return _snapshot;
    }

    private async Task<UserAccessSnapshot> ResolveAsync(CancellationToken cancellationToken)
    {
        var principal = userContext.Principal;
        var userIdClaim = principal.FindFirst(UserIdClaimType)?.Value;
        UlidId userId;

        if (!Ulid.TryParse(userIdClaim, out var parsed))
        {
            userId = await currentUserInitializer.EnsureCurrentUserIdAsync(cancellationToken);
            AddUserIdClaim(principal, userId.ToString());
        }
        else
        {
            userId = UlidId.FromUlid(parsed);
        }

        var isGlobalAdmin = await usersDbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(x =>
                    x.UserId == userId.ToString() &&
                    x.Role == UserRole.Admin &&
                    x.CustomerId == null,
                cancellationToken);

        IReadOnlyCollection<string> allowedCustomers;
        if (isGlobalAdmin)
        {
            allowedCustomers = [];
        }
        else
        {
            var list = await customersDbContext.CustomerAdmins
                .AsNoTracking()
                .Where(a => a.UserId == userId.ToString())
                .Select(a => a.CustomerId)
                .Distinct()
                .ToListAsync(cancellationToken);
            allowedCustomers = list;
        }

        return new UserAccessSnapshot(userId, isGlobalAdmin, allowedCustomers);
    }

    private static void AddUserIdClaim(ClaimsPrincipal principal, string userId)
    {
        if (principal.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        if (identity.HasClaim(c => c.Type == UserIdClaimType))
        {
            return;
        }

        identity.AddClaim(new Claim(UserIdClaimType, userId));
    }

    private sealed record UserAccessSnapshot(
        UlidId UserId,
        bool IsGlobalAdmin,
        IReadOnlyCollection<string> AllowedCustomerIds
    );
}