using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Mappers;
using Holmes.Users.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql;

/// <summary>
///     Write-focused repository for User aggregate.
///     Query methods are in SqlUserQueries (CQRS pattern).
///     Projections are updated via event handlers (UserProjectionHandler).
/// </summary>
public class UserRepository(UsersDbContext dbContext) : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        var db = UserMapper.ToDb(user);
        dbContext.Users.Add(db);
        return Task.CompletedTask;
    }

    public async Task<User?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var spec = new UserWithDetailsByIdSpecification(id.ToString());

        var db = await dbContext.Users
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        return db is null ? null : UserMapper.ToDomain(db);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        var spec = new UserWithDetailsByIdSpecification(user.Id.ToString());

        var db = await dbContext.Users
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (db is null)
        {
            throw new InvalidOperationException($"User '{user.Id}' does not exist.");
        }

        UserMapper.UpdateDb(db, user);
    }
}