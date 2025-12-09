using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Users.Application.Abstractions.Dtos;
using Holmes.Users.Application.Abstractions.Queries;
using Holmes.Users.Infrastructure.Sql.Mappers;
using Holmes.Users.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql.Queries;

public sealed class SqlUserQueries(UsersDbContext dbContext) : IUserQueries
{
    public async Task<UserDto?> GetByExternalIdentityAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken
    )
    {
        var spec = new UserByExternalIdentitySpec(issuer, subject);

        var identity = await dbContext.UserExternalIdentities
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (identity?.User is null)
        {
            return null;
        }

        var directorySpec = new UserProjectionByIdsSpecification([identity.User.UserId]);
        var directory = await dbContext.UserProjections
            .AsNoTracking()
            .ApplySpecification(directorySpec)
            .SingleOrDefaultAsync(cancellationToken);

        return UserMapper.ToDto(identity.User, directory);
    }

    public async Task<UserDto?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var spec = new UserByEmailSpec(email);

        var db = await dbContext.Users
            .Include(u => u.ExternalIdentities)
            .Include(u => u.RoleMemberships)
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (db is null)
        {
            return null;
        }

        var directorySpec = new UserProjectionByIdsSpecification([db.UserId]);
        var directory = await dbContext.UserProjections
            .AsNoTracking()
            .ApplySpecification(directorySpec)
            .SingleOrDefaultAsync(cancellationToken);

        return UserMapper.ToDto(db, directory);
    }

    public async Task<UserDto?> GetByIdAsync(UlidId userId, CancellationToken cancellationToken)
    {
        var spec = new UserWithDetailsByIdSpecification(userId.ToString());

        var db = await dbContext.Users
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        if (db is null)
        {
            return null;
        }

        var directorySpec = new UserProjectionByIdsSpecification([db.UserId]);
        var directory = await dbContext.UserProjections
            .AsNoTracking()
            .ApplySpecification(directorySpec)
            .SingleOrDefaultAsync(cancellationToken);

        return UserMapper.ToDto(db, directory);
    }

    public async Task<IReadOnlyList<UserDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        var spec = new UsersWithDetailsSpecification();

        var users = await dbContext.Users
            .ApplySpecification(spec)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.UserId).ToList();
        var directorySpec = new UserProjectionByIdsSpecification(userIds);
        var directoryEntries = await dbContext.UserProjections
            .AsNoTracking()
            .ApplySpecification(directorySpec)
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        return users.Select(u => UserMapper.ToDto(u, directoryEntries.GetValueOrDefault(u.UserId))).ToList();
    }

    public async Task<UserPagedResult> GetUsersPagedAsync(int page, int pageSize, CancellationToken cancellationToken)
    {
        var countSpec = new UsersWithDetailsSpecification();
        var totalCount = await dbContext.Users
            .AsNoTracking()
            .ApplySpecification(countSpec)
            .CountAsync(cancellationToken);

        var usersSpec = new UsersWithDetailsSpecification(page, pageSize);
        var users = await dbContext.Users
            .AsNoTracking()
            .ApplySpecification(usersSpec)
            .ToListAsync(cancellationToken);

        var userIds = users.Select(u => u.UserId).ToList();
        var directorySpec = new UserProjectionByIdsSpecification(userIds);
        var directoryEntries = await dbContext.UserProjections
            .AsNoTracking()
            .ApplySpecification(directorySpec)
            .ToDictionaryAsync(x => x.UserId, cancellationToken);

        var items = users
            .Select(u => UserMapper.ToDto(u, directoryEntries.GetValueOrDefault(u.UserId)))
            .ToList();

        return new UserPagedResult(items, totalCount);
    }

    public async Task<CurrentUserDto?> GetCurrentUserAsync(string userId, CancellationToken cancellationToken)
    {
        var directorySpec = new UserProjectionByIdsSpecification([userId]);
        var projection = await dbContext.UserProjections
            .AsNoTracking()
            .ApplySpecification(directorySpec)
            .SingleOrDefaultAsync(cancellationToken);

        if (projection is null)
        {
            return null;
        }

        var roles = await dbContext.UserRoleMemberships
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .Select(x => new UserRoleDto(x.Role, x.CustomerId))
            .ToListAsync(cancellationToken);

        return new CurrentUserDto(
            userId,
            projection.Email,
            projection.DisplayName,
            projection.Issuer,
            projection.Subject,
            projection.Status,
            projection.LastAuthenticatedAt,
            roles);
    }
}