using Holmes.Core.Domain;
using Holmes.Customers.Application.Abstractions;
using Holmes.Customers.Application.Abstractions.Dtos;
using Holmes.Customers.Application.Abstractions.Queries;
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