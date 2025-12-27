using Holmes.Core.Application;

namespace Holmes.Customers.Application.Abstractions.Queries;

public sealed record CheckCustomerExistsQuery(
    string CustomerId
) : RequestBase<bool>;