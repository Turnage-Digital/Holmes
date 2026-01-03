using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.Customers.Contracts.Dtos;

namespace Holmes.Customers.Application.Queries;

public sealed record GetCustomerAdminsQuery(
    string CustomerId
) : RequestBase<Result<IReadOnlyList<CustomerAdminDto>>>;