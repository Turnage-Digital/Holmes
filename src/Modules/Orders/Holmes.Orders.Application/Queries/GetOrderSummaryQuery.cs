using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Orders.Contracts.Dtos;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderSummaryQuery(
    string OrderId
) : RequestBase<Result<OrderSummaryDto>>;