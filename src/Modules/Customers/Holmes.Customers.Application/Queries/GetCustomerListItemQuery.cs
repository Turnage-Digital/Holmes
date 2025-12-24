using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Customers.Application.Abstractions;
using Holmes.Customers.Application.Abstractions.Dtos;
using MediatR;

namespace Holmes.Customers.Application.Queries;

public sealed record GetCustomerListItemQuery(
    string CustomerId
) : RequestBase<Result<CustomerListItemDto>>;

public sealed class GetCustomerListItemQueryHandler(
    ICustomerQueries customerQueries
) : IRequestHandler<GetCustomerListItemQuery, Result<CustomerListItemDto>>
{
    public async Task<Result<CustomerListItemDto>> Handle(
        GetCustomerListItemQuery request,
        CancellationToken cancellationToken
    )
    {
        var customer = await customerQueries.GetListItemByIdAsync(
            request.CustomerId, cancellationToken);

        if (customer is null)
        {
            return Result.Fail<CustomerListItemDto>($"Customer {request.CustomerId} not found");
        }

        return Result.Success(customer);
    }
}