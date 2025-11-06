using Holmes.Users.Domain;

namespace Holmes.Users.Infrastructure.Sql.Entities;

public class UserDb
{
    public string UserId { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string? DisplayName { get; set; }

    public UserStatus Status { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public ICollection<UserExternalIdentityDb> ExternalIdentities { get; set; } = new List<UserExternalIdentityDb>();

    public ICollection<UserRoleMembershipDb> RoleMemberships { get; set; } = new List<UserRoleMembershipDb>();
}