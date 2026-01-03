using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql;

public sealed class UserDirectory(UsersDbContext dbContext) : IUserDirectory
{
    public Task<bool> ExistsAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var id = userId.ToString();
        return dbContext.UserProjections
            .AsNoTracking()
            .AnyAsync(x => x.UserId == id, cancellationToken);
    }
}