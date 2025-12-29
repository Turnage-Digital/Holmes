using Holmes.Core.Application;

namespace Holmes.Customers.Application.Queries;

public sealed record CheckCustomerExistsQuery(
    string CustomerId
) : RequestBase<bool>;