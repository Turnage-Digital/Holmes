using Holmes.Core.Domain.ValueObjects;
using Holmes.Core.Infrastructure.Sql.Specifications;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Entities;
using Holmes.Users.Infrastructure.Sql.Mappers;
using Holmes.Users.Infrastructure.Sql.Specifications;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql.Repositories;

public class SqlUserRepository(UsersDbContext dbContext) : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        var db = UserMapper.ToDb(user);
        dbContext.Users.Add(db);
        UpsertDirectory(user, db);
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

    public async Task<User?> GetByExternalIdentityAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken
    )
    {
        var spec = new UserByExternalIdentitySpec(issuer, subject);

        var identity = await dbContext.UserExternalIdentities
            .ApplySpecification(spec)
            .FirstOrDefaultAsync(cancellationToken);

        return identity is null ? null : UserMapper.ToDomain(identity.User);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var spec = new UserByEmailSpec(email);

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
        UpsertDirectory(user, db);
    }

    private void UpsertDirectory(User user, UserDb db)
    {
        var entry = dbContext.UserDirectory.SingleOrDefault(x => x.UserId == db.UserId);
        var lastSeen = user.ExternalIdentities.Any()
            ? user.ExternalIdentities.Max(x => x.LastSeenAt)
            : user.CreatedAt;
        var primaryIdentity = user.ExternalIdentities.FirstOrDefault();
        var issuer = primaryIdentity?.Issuer ?? "urn:holmes:invite";
        var subject = primaryIdentity?.Subject ?? db.UserId;

        if (entry is null)
        {
            entry = new UserDirectoryDb
            {
                UserId = db.UserId,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Issuer = issuer,
                Subject = subject,
                LastAuthenticatedAt = lastSeen,
                Status = user.Status
            };
            dbContext.UserDirectory.Add(entry);
        }
        else
        {
            entry.Email = user.Email;
            entry.DisplayName = user.DisplayName;
            entry.Status = user.Status;
            entry.Issuer = primaryIdentity?.Issuer ?? issuer;
            entry.Subject = primaryIdentity?.Subject ?? subject;
            entry.LastAuthenticatedAt = lastSeen;
        }
    }
}
