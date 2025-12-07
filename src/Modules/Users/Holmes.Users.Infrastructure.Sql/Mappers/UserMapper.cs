using Holmes.Users.Application.Abstractions.Dtos;
using Holmes.Users.Infrastructure.Sql.Entities;

namespace Holmes.Users.Infrastructure.Sql.Mappers;

public static class UserMapper
{
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
}