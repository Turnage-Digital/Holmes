using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Orders.Contracts;

namespace Holmes.Orders.Application.Queries;

public sealed record ListOrderSummariesQuery(
    OrderSummaryFilter Filter,
    int Page,
    int PageSize
) : RequestBase<Result<OrderSummaryPagedResult>>;