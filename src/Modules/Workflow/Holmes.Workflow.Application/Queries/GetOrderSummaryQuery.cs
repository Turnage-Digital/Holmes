using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Workflow.Application.Abstractions.Dtos;
using Holmes.Workflow.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Workflow.Application.Queries;

public sealed record GetOrderSummaryQuery(
    string OrderId
) : RequestBase<Result<OrderSummaryDto>>;

public sealed class GetOrderSummaryQueryHandler(
    IOrderQueries orderQueries
) : IRequestHandler<GetOrderSummaryQuery, Result<OrderSummaryDto>>
{
    public async Task<Result<OrderSummaryDto>> Handle(
        GetOrderSummaryQuery request,
        CancellationToken cancellationToken
    )
    {
        var order = await orderQueries.GetSummaryByIdAsync(request.OrderId, cancellationToken);

        if (order is null)
        {
            return Result.Fail<OrderSummaryDto>($"Order {request.OrderId} not found");
        }

        return Result.Success(order);
    }
}