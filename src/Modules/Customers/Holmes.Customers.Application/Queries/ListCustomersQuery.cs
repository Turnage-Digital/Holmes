using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Customers.Application.Abstractions;
using Holmes.Customers.Application.Abstractions.Dtos;

namespace Holmes.Customers.Application.Queries;

public sealed record ListCustomersQuery(
    IReadOnlyCollection<string>? AllowedCustomerIds,
    int Page,
    int PageSize
) : RequestBase<Result<CustomerPagedResult>>;
