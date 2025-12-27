using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions;
using MediatR;
using Holmes.Customers.Application.Abstractions.Queries;

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
