using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions.Dtos;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderTimelineQuery(
    string OrderId,
    DateTimeOffset? Before,
    int Limit
) : RequestBase<Result<IReadOnlyList<OrderTimelineEntryDto>>>;