using Holmes.Core.Application;
using Holmes.Core.Domain;

namespace Holmes.Customers.Application.Abstractions.Queries;

public sealed record ListCustomersQuery(
    IReadOnlyCollection<string>? AllowedCustomerIds,
    int Page,
    int PageSize
) : RequestBase<Result<CustomerPagedResult>>;