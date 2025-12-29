using Holmes.Core.Application;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderCustomerIdQuery(
    string OrderId
) : RequestBase<string?>;