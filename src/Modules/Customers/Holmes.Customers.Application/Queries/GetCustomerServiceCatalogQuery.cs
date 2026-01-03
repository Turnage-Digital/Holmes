using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.Services.Contracts.Dtos;

namespace Holmes.Customers.Application.Queries;

/// <summary>
///     Gets the service catalog configuration for a customer.
///     This is a cross-module query that delegates to the Services module.
/// </summary>
public sealed record GetCustomerServiceCatalogQuery(
    string CustomerId
) : RequestBase<Result<CustomerServiceCatalogDto>>;