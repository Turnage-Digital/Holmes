using Holmes.Core.Application;
using Holmes.Workflow.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Workflow.Application.Queries;

public sealed record GetOrderCustomerIdQuery(
    string OrderId
) : RequestBase<string?>;

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