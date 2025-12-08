using Holmes.Core.Domain.Specifications;
using Holmes.Customers.Infrastructure.Sql.Entities;

namespace Holmes.Customers.Infrastructure.Sql.Specifications;

public sealed class CustomerWithAdminsByIdSpec : Specification<CustomerDb>
{
    public CustomerWithAdminsByIdSpec(string customerId)
    {
        AddCriteria(c => c.CustomerId == customerId);
        AddInclude(c => c.Admins);
    }
}
