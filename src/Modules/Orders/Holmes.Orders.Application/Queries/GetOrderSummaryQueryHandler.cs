using Holmes.Core.Domain;
using Holmes.Orders.Contracts;
using Holmes.Orders.Contracts.Dtos;
using MediatR;

namespace Holmes.Orders.Application.Queries;

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