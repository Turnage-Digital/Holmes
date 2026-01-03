using Holmes.Core.Application;
using Holmes.Customers.Contracts;
using Holmes.Customers.Contracts.Dtos;
using MediatR;

namespace Holmes.Customers.Application.Queries;

public sealed class GetCustomerAdminsQueryHandler(
    ICustomerQueries customerQueries
) : IRequestHandler<GetCustomerAdminsQuery, Result<IReadOnlyList<CustomerAdminDto>>>
{
    public async Task<Result<IReadOnlyList<CustomerAdminDto>>> Handle(
        GetCustomerAdminsQuery request,
        CancellationToken cancellationToken
    )
    {
        var admins = await customerQueries.GetAdminsAsync(
            request.CustomerId, cancellationToken);

        return Result.Success(admins);
    }
}