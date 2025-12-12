using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Workflow.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Workflow.Application.Queries;

public sealed record ListOrderSummariesQuery(
    OrderSummaryFilter Filter,
    int Page,
    int PageSize
) : RequestBase<Result<OrderSummaryPagedResult>>;

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