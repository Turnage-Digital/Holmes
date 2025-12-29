using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions;

namespace Holmes.Orders.Application.Queries;

public sealed record ListOrderSummariesQuery(
    OrderSummaryFilter Filter,
    int Page,
    int PageSize
) : RequestBase<Result<OrderSummaryPagedResult>>;