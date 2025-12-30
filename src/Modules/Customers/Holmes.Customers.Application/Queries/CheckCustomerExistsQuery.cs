using Holmes.Core.Contracts;

namespace Holmes.Customers.Application.Queries;

public sealed record CheckCustomerExistsQuery(
    string CustomerId
) : RequestBase<bool>;