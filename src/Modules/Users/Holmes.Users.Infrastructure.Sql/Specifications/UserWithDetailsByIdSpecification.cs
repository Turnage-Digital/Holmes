using Holmes.Core.Domain.Specifications;
using Holmes.Users.Infrastructure.Sql.Entities;

namespace Holmes.Users.Infrastructure.Sql.Specifications;

public sealed class UserWithDetailsByIdSpecification : Specification<UserDb>
{
    public UserWithDetailsByIdSpecification(string userId)
    {
        AddCriteria(u => u.UserId == userId);
        AddInclude(u => u.ExternalIdentities);
        AddInclude(u => u.RoleMemberships);
    }
}