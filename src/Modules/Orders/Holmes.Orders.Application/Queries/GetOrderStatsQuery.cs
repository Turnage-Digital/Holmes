using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Orders.Contracts.Dtos;

namespace Holmes.Orders.Application.Queries;

public sealed record GetOrderStatsQuery(
    IReadOnlyCollection<string>? AllowedCustomerIds
) : RequestBase<Result<OrderStatsDto>>;