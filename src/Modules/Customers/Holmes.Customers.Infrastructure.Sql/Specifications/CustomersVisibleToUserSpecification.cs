using Holmes.Core.Domain.Specifications;
using Holmes.Customers.Infrastructure.Sql.Entities;

namespace Holmes.Customers.Infrastructure.Sql.Specifications;

public sealed class CustomersVisibleToUserSpecification : Specification<CustomerDirectoryDb>
{
    public CustomersVisibleToUserSpecification(
        IEnumerable<string>? allowedCustomerIds,
        int? page = null,
        int? pageSize = null
    )
    {
        if (allowedCustomerIds is not null)
        {
            var ids = allowedCustomerIds.ToArray();
            if (ids.Length > 0)
            {
                AddCriteria(c => ids.Contains(c.CustomerId));
            }
            else
            {
                AddCriteria(_ => false);
            }
        }

        ApplyOrderBy(c => c.Name);
        if (page.HasValue && pageSize.HasValue)
        {
            var skip = (page.Value - 1) * pageSize.Value;
            ApplyPaging(skip, pageSize.Value);
        }
    }
}
