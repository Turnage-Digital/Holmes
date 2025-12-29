using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.Customers.Application.Abstractions.Dtos;

namespace Holmes.Customers.Application.Queries;

public sealed record GetCustomerAdminsQuery(
    string CustomerId
) : RequestBase<Result<IReadOnlyList<CustomerAdminDto>>>;