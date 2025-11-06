using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql.Repositories;

public sealed class SqlUserDirectory : IUserDirectory
{
    private readonly UsersDbContext _dbContext;

    public SqlUserDirectory(UsersDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<bool> ExistsAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var id = userId.ToString();
        return _dbContext.UserDirectory
            .AsNoTracking()
            .AnyAsync(x => x.UserId == id, cancellationToken);
    }
}