using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.Orders.Contracts.Dtos;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderTimelineQuery(
    string OrderId,
    DateTimeOffset? Before,
    int Limit
) : RequestBase<Result<IReadOnlyList<OrderTimelineEntryDto>>>;