using Holmes.Orders.Application.Abstractions;
using Holmes.Orders.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Orders.Application.Queries;

public sealed class GetOrderCustomerIdQueryHandler(
    IOrderQueries orderQueries
) : IRequestHandler<GetOrderCustomerIdQuery, string?>
{
    public async Task<string?> Handle(
        GetOrderCustomerIdQuery request,
        CancellationToken cancellationToken
    )
    {
        return await orderQueries.GetCustomerIdAsync(request.OrderId, cancellationToken);
    }
}
