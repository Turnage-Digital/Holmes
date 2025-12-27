using Holmes.Customers.Application.Abstractions;
using Holmes.Customers.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Customers.Application.Queries;

public sealed class CheckCustomerExistsQueryHandler(
    ICustomerQueries customerQueries
) : IRequestHandler<CheckCustomerExistsQuery, bool>
{
    public async Task<bool> Handle(
        CheckCustomerExistsQuery request,
        CancellationToken cancellationToken
    )
    {
        return await customerQueries.ExistsAsync(request.CustomerId, cancellationToken);
    }
}
