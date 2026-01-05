using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Customers.Contracts;
using Holmes.Orders.Contracts.Dtos;
using Holmes.Orders.Domain;
using Holmes.Users.Contracts;
using MediatR;

namespace Holmes.Orders.Application.Commands;

public sealed class CreateOrderCommandHandler(
    IOrdersUnitOfWork unitOfWork,
    ICustomerQueries customerQueries,
    IUserAccessQueries userAccessQueries,
    ICustomerAccessQueries customerAccessQueries
) : IRequestHandler<CreateOrderCommand, Result<CreateOrderResponse>>
{
    public async Task<Result<CreateOrderResponse>> Handle(
        CreateOrderCommand request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.SubjectEmail))
        {
            return Result.Fail<CreateOrderResponse>(ResultErrors.Validation);
        }

        var userId = request.GetUserUlid();
        var isGlobalAdmin = await userAccessQueries.IsGlobalAdminAsync(userId, cancellationToken);
        if (!isGlobalAdmin)
        {
            var allowedCustomers = await customerAccessQueries.GetAdminCustomerIdsAsync(userId, cancellationToken);
            if (!allowedCustomers.Contains(request.CustomerId.ToString()))
            {
                return Result.Fail<CreateOrderResponse>(ResultErrors.Forbidden);
            }
        }

        var customerExists = await customerQueries.ExistsAsync(
            request.CustomerId.ToString(),
            cancellationToken);
        if (!customerExists)
        {
            return Result.Fail<CreateOrderResponse>(ResultErrors.NotFound);
        }

        var orderId = UlidId.NewUlid();
        var order = Order.Create(
            orderId,
            request.CustomerId,
            request.PolicySnapshotId,
            request.SubjectEmail,
            request.SubjectPhone,
            request.CreatedAt,
            request.PackageCode,
            userId);

        await unitOfWork.Orders.AddAsync(order, cancellationToken);
        await unitOfWork.SaveChangesAsync(true, cancellationToken);

        return Result.Success(new CreateOrderResponse(orderId.ToString()));
    }
}
