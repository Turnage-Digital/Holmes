using Holmes.Core.Application;
using Holmes.Customers.Application.Abstractions;
using MediatR;

namespace Holmes.Customers.Application.Queries;

public sealed record CheckCustomerExistsQuery(
    string CustomerId
) : RequestBase<bool>;

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