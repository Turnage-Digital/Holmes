using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions.Dtos;

namespace Holmes.Orders.Application.Abstractions.Queries;

public sealed record GetOrderSummaryQuery(
    string OrderId
) : RequestBase<Result<OrderSummaryDto>>;