using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Customers.Application.Abstractions.Dtos;

namespace Holmes.Customers.Application.Abstractions.Queries;

public sealed record GetCustomerListItemQuery(
    string CustomerId
) : RequestBase<Result<CustomerListItemDto>>;