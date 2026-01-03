using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Customers.Contracts;

namespace Holmes.Customers.Application.Queries;

public sealed record ListCustomersQuery(
    IReadOnlyCollection<string>? AllowedCustomerIds,
    int Page,
    int PageSize
) : RequestBase<Result<CustomerPagedResult>>;