using Holmes.Core.Application;
using Holmes.Customers.Contracts;
using Holmes.Customers.Contracts.Dtos;
using MediatR;

namespace Holmes.Customers.Application.Queries;

public sealed class GetCustomerByIdQueryHandler(
    ICustomerQueries customerQueries
) : IRequestHandler<GetCustomerByIdQuery, Result<CustomerDetailDto>>
{
    public async Task<Result<CustomerDetailDto>> Handle(
        GetCustomerByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var customer = await customerQueries.GetByIdAsync(
            request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Result.Fail<CustomerDetailDto>($"Customer {request.CustomerId} not found");
        }

        return Result.Success(customer);
    }
}