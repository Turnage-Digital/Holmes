using Holmes.Core.Application.Abstractions;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderCustomerIdQuery(
    string OrderId
) : RequestBase<string?>;