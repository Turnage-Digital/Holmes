using System.Linq;
using Holmes.Core.Domain.Specifications;
using Holmes.Users.Infrastructure.Sql.Entities;

namespace Holmes.Users.Infrastructure.Sql.Specifications;

public sealed class UserDirectoryByIdsSpecification : Specification<UserDirectoryDb>
{
    public UserDirectoryByIdsSpecification(IEnumerable<string> userIds)
    {
        var ids = userIds.ToArray();
        if (ids.Length > 0)
        {
            AddCriteria(entry => ids.Contains(entry.UserId));
        }
    }
}
