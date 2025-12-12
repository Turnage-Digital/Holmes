using Holmes.Core.Domain.Specifications;
using Holmes.Users.Infrastructure.Sql.Entities;

namespace Holmes.Users.Infrastructure.Sql.Specifications;

public sealed class UserByExternalIdentitySpec : Specification<UserExternalIdentityDb>
{
    public UserByExternalIdentitySpec(string issuer, string subject)
    {
        AddCriteria(x => x.Issuer == issuer && x.Subject == subject);
        AddInclude("User");
        AddInclude("User.ExternalIdentities");
        AddInclude("User.RoleMemberships");
    }
}