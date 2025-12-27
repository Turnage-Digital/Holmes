using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions.Dtos;

namespace Holmes.Customers.Application.Abstractions.Queries;

/// <summary>
///     Gets the service catalog configuration for a customer.
///     This is a cross-module query that delegates to the Services module.
/// </summary>
public sealed record GetCustomerServiceCatalogQuery(
    string CustomerId
) : RequestBase<Result<CustomerServiceCatalogDto>>;