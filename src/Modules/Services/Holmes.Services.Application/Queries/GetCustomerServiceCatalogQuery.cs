using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Services.Contracts.Dtos;

namespace Holmes.Services.Application.Queries;

public sealed record GetCustomerServiceCatalogQuery(
    string CustomerId
) : RequestBase<Result<CustomerServiceCatalogDto>>;