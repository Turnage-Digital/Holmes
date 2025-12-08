using Holmes.Core.Domain.ValueObjects;
using Holmes.Users.Application.Abstractions.Dtos;
using Holmes.Users.Domain;
using Holmes.Users.Infrastructure.Sql.Entities;

namespace Holmes.Users.Infrastructure.Sql.Mappers;

public static class UserMapper
{
    public static User ToDomain(UserDb db)
    {
        var identities = db.ExternalIdentities
            .Select(e =>
                ExternalIdentity.Restore(e.Issuer, e.Subject, e.AuthenticationMethod, e.LinkedAt, e.LastSeenAt));

        var roles = db.RoleMemberships
            .Select(r => new RoleAssignment(r.Role, r.CustomerId, r.GrantedBy, r.GrantedAt));

        return User.Rehydrate(
            UlidId.Parse(db.UserId),
            db.Email,
            db.DisplayName,
            db.Status,
            db.CreatedAt,
            identities,
            roles);
    }

    public static UserDb ToDb(User user)
    {
        var db = new UserDb
        {
            UserId = user.Id.ToString(),
            Email = user.Email,
            DisplayName = user.DisplayName,
            Status = user.Status,
            CreatedAt = user.CreatedAt
        };

        foreach (var identity in user.ExternalIdentities)
        {
            db.ExternalIdentities.Add(new UserExternalIdentityDb
            {
                UserId = db.UserId,
                Issuer = identity.Issuer,
                Subject = identity.Subject,
                AuthenticationMethod = identity.AuthenticationMethod,
                LinkedAt = identity.LinkedAt,
                LastSeenAt = identity.LastSeenAt
            });
        }

        foreach (var role in user.Roles)
        {
            db.RoleMemberships.Add(new UserRoleMembershipDb
            {
                UserId = db.UserId,
                Role = role.Role,
                CustomerId = role.CustomerId,
                GrantedBy = role.GrantedBy,
                GrantedAt = role.GrantedAt
            });
        }

        return db;
    }

    public static void UpdateDb(UserDb db, User user)
    {
        db.Email = user.Email;
        db.DisplayName = user.DisplayName;
        db.Status = user.Status;

        SyncExternalIdentities(db, user);
        SyncRoles(db, user);
    }

    public static UserDto ToDto(UserDb user, UserDirectoryDb? directory)
    {
        var primaryIdentity = user.ExternalIdentities
            .OrderByDescending(x => x.LastSeenAt)
            .FirstOrDefault();

        var identity = primaryIdentity is null
            ? null
            : new ExternalIdentityDto(
                primaryIdentity.Issuer,
                primaryIdentity.Subject,
                primaryIdentity.AuthenticationMethod,
                primaryIdentity.LinkedAt,
                primaryIdentity.LastSeenAt);

        var roles = user.RoleMemberships
            .OrderByDescending(r => r.GrantedAt)
            .Select(r => new RoleAssignmentDto(
                r.Id.ToString(),
                r.Role,
                r.CustomerId,
                r.GrantedBy.ToString(),
                r.GrantedAt))
            .ToList();

        var lastSeen = directory?.LastAuthenticatedAt ?? user.CreatedAt;

        return new UserDto(
            user.UserId,
            user.Email,
            user.DisplayName,
            user.Status,
            lastSeen,
            user.CreatedAt,
            user.CreatedAt,
            roles,
            identity);
    }

    private static void SyncExternalIdentities(UserDb db, User user)
    {
        var desired = user.ExternalIdentities.ToDictionary(x => (x.Issuer, x.Subject), x => x);
        var existing = db.ExternalIdentities.ToDictionary(x => (x.Issuer, x.Subject), x => x);

        foreach (var toRemove in existing.Keys.Except(desired.Keys).ToList())
        {
            var recordToRemove = existing[toRemove];
            db.ExternalIdentities.Remove(recordToRemove);
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
                db.ExternalIdentities.Add(new UserExternalIdentityDb
                {
                    UserId = db.UserId,
                    Issuer = kvp.Value.Issuer,
                    Subject = kvp.Value.Subject,
                    AuthenticationMethod = kvp.Value.AuthenticationMethod,
                    LinkedAt = kvp.Value.LinkedAt,
                    LastSeenAt = kvp.Value.LastSeenAt
                });
            }
        }
    }

    private static void SyncRoles(UserDb db, User user)
    {
        var desired = user.Roles.ToDictionary(x => (x.Role, x.CustomerId), x => x);
        var existing = db.RoleMemberships.ToDictionary(x => (x.Role, x.CustomerId), x => x);

        foreach (var toRemove in existing.Keys.Except(desired.Keys).ToList())
        {
            var membership = existing[toRemove];
            db.RoleMemberships.Remove(membership);
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
                db.RoleMemberships.Add(new UserRoleMembershipDb
                {
                    UserId = db.UserId,
                    Role = kvp.Value.Role,
                    CustomerId = kvp.Value.CustomerId,
                    GrantedBy = kvp.Value.GrantedBy,
                    GrantedAt = kvp.Value.GrantedAt
                });
            }
        }
    }
}