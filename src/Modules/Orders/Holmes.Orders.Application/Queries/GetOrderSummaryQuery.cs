using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions.Dtos;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderSummaryQuery(
    string OrderId
) : RequestBase<Result<OrderSummaryDto>>;