using Holmes.Core.Domain;
using Holmes.Orders.Contracts;
using Holmes.Orders.Contracts.Dtos;
using MediatR;

namespace Holmes.Orders.Application.Queries;

public sealed class GetOrderStatsQueryHandler(
    IOrderQueries orderQueries
) : IRequestHandler<GetOrderStatsQuery, Result<OrderStatsDto>>
{
    public async Task<Result<OrderStatsDto>> Handle(
        GetOrderStatsQuery request,
        CancellationToken cancellationToken
    )
    {
        var stats = await orderQueries.GetStatsAsync(
            request.AllowedCustomerIds, cancellationToken);

        return Result.Success(stats);
    }
}