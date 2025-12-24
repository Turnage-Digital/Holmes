using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions.Dtos;
using Holmes.Orders.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderTimelineQuery(
    string OrderId,
    DateTimeOffset? Before,
    int Limit
) : RequestBase<Result<IReadOnlyList<OrderTimelineEntryDto>>>;

public sealed class GetOrderTimelineQueryHandler(
    IOrderQueries orderQueries
) : IRequestHandler<GetOrderTimelineQuery, Result<IReadOnlyList<OrderTimelineEntryDto>>>
{
    public async Task<Result<IReadOnlyList<OrderTimelineEntryDto>>> Handle(
        GetOrderTimelineQuery request,
        CancellationToken cancellationToken
    )
    {
        var timeline = await orderQueries.GetTimelineAsync(
            request.OrderId,
            request.Before,
            request.Limit,
            cancellationToken);

        return Result.Success(timeline);
    }
}