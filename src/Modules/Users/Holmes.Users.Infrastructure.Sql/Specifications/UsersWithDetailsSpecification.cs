using Holmes.Core.Domain.Specifications;
using Holmes.Users.Infrastructure.Sql.Entities;

namespace Holmes.Users.Infrastructure.Sql.Specifications;

public sealed class UsersWithDetailsSpecification : Specification<UserDb>
{
    public UsersWithDetailsSpecification(int? page = null, int? pageSize = null)
    {
        AddInclude(u => u.ExternalIdentities);
        AddInclude(u => u.RoleMemberships);
        ApplyOrderBy(u => u.Email);
        if (page.HasValue && pageSize.HasValue)
        {
            var skip = (page.Value - 1) * pageSize.Value;
            ApplyPaging(skip, pageSize.Value);
        }
    }
}