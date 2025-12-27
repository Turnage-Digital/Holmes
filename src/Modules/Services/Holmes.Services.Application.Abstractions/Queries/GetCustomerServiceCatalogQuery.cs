using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions.Dtos;

namespace Holmes.Services.Application.Abstractions.Queries;

public sealed record GetCustomerServiceCatalogQuery(
    string CustomerId
) : RequestBase<Result<CustomerServiceCatalogDto>>;