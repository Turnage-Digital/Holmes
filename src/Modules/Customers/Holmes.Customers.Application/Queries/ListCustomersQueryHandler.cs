using Holmes.Core.Domain;
using Holmes.Customers.Application.Abstractions;
using Holmes.Customers.Application.Queries;
using MediatR;

namespace Holmes.Customers.Application.Queries;

public sealed class ListCustomersQueryHandler(
    ICustomerQueries customerQueries
) : IRequestHandler<ListCustomersQuery, Result<CustomerPagedResult>>
{
    public async Task<Result<CustomerPagedResult>> Handle(
        ListCustomersQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await customerQueries.GetCustomersPagedAsync(
            request.AllowedCustomerIds,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}