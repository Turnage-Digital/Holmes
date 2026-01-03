using Holmes.Core.Application;
using Holmes.Orders.Contracts;
using MediatR;

namespace Holmes.Orders.Application.Queries;

public sealed class ListOrderSummariesQueryHandler(
    IOrderQueries orderQueries
) : IRequestHandler<ListOrderSummariesQuery, Result<OrderSummaryPagedResult>>
{
    public async Task<Result<OrderSummaryPagedResult>> Handle(
        ListOrderSummariesQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await orderQueries.GetSummariesPagedAsync(
            request.Filter,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}