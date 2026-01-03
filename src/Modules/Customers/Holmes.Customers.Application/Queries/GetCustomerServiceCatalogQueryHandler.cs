using Holmes.Core.Application;
using Holmes.Services.Contracts;
using Holmes.Services.Contracts.Dtos;
using MediatR;

namespace Holmes.Customers.Application.Queries;

public sealed class GetCustomerServiceCatalogQueryHandler(
    IServiceCatalogQueries serviceCatalogQueries
) : IRequestHandler<GetCustomerServiceCatalogQuery, Result<CustomerServiceCatalogDto>>
{
    public async Task<Result<CustomerServiceCatalogDto>> Handle(
        GetCustomerServiceCatalogQuery request,
        CancellationToken cancellationToken
    )
    {
        var catalog = await serviceCatalogQueries.GetByCustomerIdAsync(
            request.CustomerId, cancellationToken);

        return Result.Success(catalog);
    }
}