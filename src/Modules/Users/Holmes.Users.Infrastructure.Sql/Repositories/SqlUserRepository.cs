using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Entities;
using Microsoft.EntityFrameworkCore;

namespace Holmes.Users.Infrastructure.Sql.Repositories;

public class SqlUserRepository(UsersDbContext dbContext) : IUserRepository
{
    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        var record = ToDb(user);
        dbContext.Users.Add(record);
        UpsertDirectory(user, record);
        return Task.CompletedTask;
    }

    public async Task<User?> GetByIdAsync(UlidId id, CancellationToken cancellationToken)
    {
        var userId = id.ToString();
        var record = await dbContext.Users
            .Include(x => x.ExternalIdentities)
            .Include(x => x.RoleMemberships)
            .FirstOrDefaultAsync(x => x.UserId == userId, cancellationToken);

        return record is null ? null : Rehydrate(record);
    }

    public async Task<User?> GetByExternalIdentityAsync(
        string issuer,
        string subject,
        CancellationToken cancellationToken
    )
    {
        var identity = await dbContext.UserExternalIdentities
            .Include(x => x.User)
            .ThenInclude(u => u!.ExternalIdentities)
            .Include(x => x.User)
            .ThenInclude(u => u!.RoleMemberships)
            .FirstOrDefaultAsync(x => x.Issuer == issuer && x.Subject == subject, cancellationToken);

        return identity is null ? null : Rehydrate(identity.User);
    }

    public async Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var record = await dbContext.Users
            .Include(x => x.ExternalIdentities)
            .Include(x => x.RoleMemberships)
            .FirstOrDefaultAsync(x => x.Email == email, cancellationToken);

        return record is null ? null : Rehydrate(record);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken)
    {
        var record = await dbContext.Users
            .Include(x => x.ExternalIdentities)
            .Include(x => x.RoleMemberships)
            .FirstOrDefaultAsync(x => x.UserId == user.Id.ToString(), cancellationToken);

        if (record is null)
        {
            throw new InvalidOperationException($"User '{user.Id}' does not exist.");
        }

        ApplyState(user, record);
        UpsertDirectory(user, record);
    }

    private static User Rehydrate(UserDb record)
    {
        var identities = record.ExternalIdentities
            .Select(e =>
                ExternalIdentity.Restore(e.Issuer, e.Subject, e.AuthenticationMethod, e.LinkedAt, e.LastSeenAt));

        var roles = record.RoleMemberships
            .Select(r => new RoleAssignment(r.Role, r.CustomerId, r.GrantedBy, r.GrantedAt));

        return User.Rehydrate(
            UlidId.Parse(record.UserId),
            record.Email,
            record.DisplayName,
            record.Status,
            record.CreatedAt,
            identities,
            roles);
    }

    private static void ApplyState(User user, UserDb record)
    {
        record.Email = user.Email;
        record.DisplayName = user.DisplayName;
        record.Status = user.Status;

        SyncExternalIdentities(user, record);
        SyncRoles(user, record);
    }

    private static void SyncExternalIdentities(User user, UserDb record)
    {
        var desired = user.ExternalIdentities.ToDictionary(x => (x.Issuer, x.Subject), x => x);
        var existing = record.ExternalIdentities.ToDictionary(x => (x.Issuer, x.Subject), x => x);

        foreach (var toRemove in existing.Keys.Except(desired.Keys).ToList())
        {
            var recordToRemove = existing[toRemove];
            record.ExternalIdentities.Remove(recordToRemove);
        }

        foreach (var kvp in desired)
        {
            if (existing.TryGetValue(kvp.Key, out var recordIdentity))
            {
                recordIdentity.AuthenticationMethod = kvp.Value.AuthenticationMethod;
                recordIdentity.LinkedAt = kvp.Value.LinkedAt;
                recordIdentity.LastSeenAt = kvp.Value.LastSeenAt;
            }
            else
            {
                record.ExternalIdentities.Add(new UserExternalIdentityDb
                {
                    UserId = record.UserId,
                    Issuer = kvp.Value.Issuer,
                    Subject = kvp.Value.Subject,
                    AuthenticationMethod = kvp.Value.AuthenticationMethod,
                    LinkedAt = kvp.Value.LinkedAt,
                    LastSeenAt = kvp.Value.LastSeenAt
                });
            }
        }
    }

    private static void SyncRoles(User user, UserDb record)
    {
        var desired = user.Roles.ToDictionary(x => (x.Role, x.CustomerId), x => x);
        var existing = record.RoleMemberships.ToDictionary(x => (x.Role, x.CustomerId), x => x);

        foreach (var toRemove in existing.Keys.Except(desired.Keys).ToList())
        {
            var membership = existing[toRemove];
            record.RoleMemberships.Remove(membership);
        }

        foreach (var kvp in desired)
        {
            if (existing.TryGetValue(kvp.Key, out var membership))
            {
                membership.GrantedBy = kvp.Value.GrantedBy;
                membership.GrantedAt = kvp.Value.GrantedAt;
            }
            else
            {
                record.RoleMemberships.Add(new UserRoleMembershipDb
                {
                    UserId = record.UserId,
                    Role = kvp.Value.Role,
                    CustomerId = kvp.Value.CustomerId,
                    GrantedBy = kvp.Value.GrantedBy,
                    GrantedAt = kvp.Value.GrantedAt
                });
            }
        }
    }

    private static UserDb ToDb(User user)
    {
        var record = new UserDb
        {
            UserId = user.Id.ToString(),
            Email = user.Email,
            DisplayName = user.DisplayName,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };

        foreach (var identity in user.ExternalIdentities)
        {
            record.ExternalIdentities.Add(new UserExternalIdentityDb
            {
                UserId = record.UserId,
                Issuer = identity.Issuer,
                Subject = identity.Subject,
                AuthenticationMethod = identity.AuthenticationMethod,
                LinkedAt = identity.LinkedAt,
                LastSeenAt = identity.LastSeenAt
            });
        }

        foreach (var role in user.Roles)
        {
            record.RoleMemberships.Add(new UserRoleMembershipDb
            {
                UserId = record.UserId,
                Role = role.Role,
                CustomerId = role.CustomerId,
                GrantedBy = role.GrantedBy,
                GrantedAt = role.GrantedAt
            });
        }

        return record;
    }

    private void UpsertDirectory(User user, UserDb record)
    {
        var entry = dbContext.UserDirectory.SingleOrDefault(x => x.UserId == record.UserId);
        var lastSeen = user.ExternalIdentities.Any()
            ? user.ExternalIdentities.Max(x => x.LastSeenAt)
            : user.CreatedAt;
        var primaryIdentity = user.ExternalIdentities.FirstOrDefault();
        var issuer = primaryIdentity?.Issuer ?? "urn:holmes:invite";
        var subject = primaryIdentity?.Subject ?? record.UserId;

        if (entry is null)
        {
            entry = new UserDirectoryProjectionDb
            {
                UserId = record.UserId,
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
