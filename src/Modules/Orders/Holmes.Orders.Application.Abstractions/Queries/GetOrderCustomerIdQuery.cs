using Holmes.Core.Application;

namespace Holmes.Orders.Application.Abstractions.Queries;

public sealed record GetOrderCustomerIdQuery(
    string OrderId
) : RequestBase<string?>;