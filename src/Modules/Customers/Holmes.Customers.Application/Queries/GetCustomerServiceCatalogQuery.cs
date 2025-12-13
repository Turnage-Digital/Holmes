using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Customers.Application.Queries;

/// <summary>
///     Gets the service catalog configuration for a customer.
///     This is a cross-module query that delegates to the Services module.
/// </summary>
public sealed record GetCustomerServiceCatalogQuery(
    string CustomerId
) : RequestBase<Result<CustomerServiceCatalogDto>>;

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
