using Holmes.Core.Contracts;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderCustomerIdQuery(
    string OrderId
) : RequestBase<string?>;