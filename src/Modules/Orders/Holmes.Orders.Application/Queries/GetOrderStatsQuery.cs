using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Orders.Application.Abstractions.Dtos;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderStatsQuery(
    IReadOnlyCollection<string>? AllowedCustomerIds
) : RequestBase<Result<OrderStatsDto>>;