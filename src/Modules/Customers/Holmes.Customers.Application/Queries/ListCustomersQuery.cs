using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Customers.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Customers.Application.Queries;

public sealed record ListCustomersQuery(
    IReadOnlyCollection<string>? AllowedCustomerIds,
    int Page,
    int PageSize
) : RequestBase<Result<CustomerPagedResult>>;

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