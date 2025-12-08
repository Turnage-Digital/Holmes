using Holmes.Core.Domain.Specifications;
using Holmes.Users.Infrastructure.Sql.Entities;

namespace Holmes.Users.Infrastructure.Sql.Specifications;

public sealed class UserByEmailSpec : Specification<UserDb>
{
    public UserByEmailSpec(string email)
    {
        AddCriteria(u => u.Email == email);
        AddInclude(u => u.ExternalIdentities);
        AddInclude(u => u.RoleMemberships);
    }
}