using Holmes.Core.Application;
using Holmes.Core.Domain;

namespace Holmes.Orders.Application.Abstractions.Queries;

public sealed record ListOrderSummariesQuery(
    OrderSummaryFilter Filter,
    int Page,
    int PageSize
) : RequestBase<Result<OrderSummaryPagedResult>>;