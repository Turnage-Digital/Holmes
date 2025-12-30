using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Contracts;
using Holmes.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql;

public sealed class UserAccessQueries(UsersDbContext dbContext) : IUserAccessQueries
{
    public async Task<bool> IsGlobalAdminAsync(UlidId userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .AnyAsync(
                x => x.UserId == userId.ToString() &&
                     x.Role == UserRole.Admin &&
                     x.CustomerId == null,
                cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetGlobalRolesAsync(UlidId userId, CancellationToken cancellationToken)
    {
        return await dbContext.UserRoleMemberships
            .AsNoTracking()
            .Where(x => x.UserId == userId.ToString() && x.CustomerId == null)
            .Select(x => x.Role.ToString())
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}