using Holmes.Core.Application.Abstractions;

namespace Holmes.Customers.Application.Queries;

public sealed record CheckCustomerExistsQuery(
    string CustomerId
) : RequestBase<bool>;