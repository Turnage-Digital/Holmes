using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Workflow.Application.Abstractions.Dtos;
using Holmes.Workflow.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Workflow.Application.Queries;

public sealed record GetOrderStatsQuery(
    IReadOnlyCollection<string>? AllowedCustomerIds
) : RequestBase<Result<OrderStatsDto>>;

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