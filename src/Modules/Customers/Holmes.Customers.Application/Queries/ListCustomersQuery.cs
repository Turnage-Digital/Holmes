using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Customers.Application.Abstractions;

namespace Holmes.Customers.Application.Queries;

public sealed record ListCustomersQuery(
    IReadOnlyCollection<string>? AllowedCustomerIds,
    int Page,
    int PageSize
) : RequestBase<Result<CustomerPagedResult>>;