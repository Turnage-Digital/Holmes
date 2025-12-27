using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions;
using Holmes.Orders.Application.Abstractions.Dtos;
using Holmes.Orders.Application.Abstractions.Queries;
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