using Holmes.Core.Application;
using Holmes.Customers.Contracts;
using Holmes.Services.Domain;
using Holmes.Users.Contracts;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed class RetryServiceCommandHandler(
    IServicesUnitOfWork unitOfWork,
    IUserAccessQueries userAccessQueries,
    ICustomerAccessQueries customerAccessQueries
) : IRequestHandler<RetryServiceCommand, Result>
{
    public async Task<Result> Handle(
        RetryServiceCommand request,
        CancellationToken cancellationToken
    )
    {
        var service = await unitOfWork.Services.GetByIdAsync(
            request.ServiceId, cancellationToken);

        if (service is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        var actor = request.GetUserUlid();
        var isGlobalAdmin = await userAccessQueries.IsGlobalAdminAsync(actor, cancellationToken);
        if (!isGlobalAdmin)
        {
            var allowedCustomers = await customerAccessQueries.GetAdminCustomerIdsAsync(actor, cancellationToken);
            if (!allowedCustomers.Contains(service.CustomerId.ToString()))
            {
                return Result.Fail(ResultErrors.Forbidden);
            }
        }

        if (!service.CanRetry)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        service.Retry(request.RetriedAt);
        unitOfWork.Services.Update(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}