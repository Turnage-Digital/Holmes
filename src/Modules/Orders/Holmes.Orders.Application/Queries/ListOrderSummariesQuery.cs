using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions;
using Holmes.Orders.Application.Abstractions.Dtos;

namespace Holmes.Orders.Application.Queries;

public sealed record ListOrderSummariesQuery(
    OrderSummaryFilter Filter,
    int Page,
    int PageSize
) : RequestBase<Result<OrderSummaryPagedResult>>;
