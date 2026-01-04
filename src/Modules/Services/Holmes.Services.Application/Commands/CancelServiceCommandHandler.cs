using Holmes.Core.Application;
using Holmes.Customers.Contracts;
using Holmes.Services.Domain;
using Holmes.Users.Contracts;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed class CancelServiceCommandHandler(
    IServicesUnitOfWork unitOfWork,
    IUserAccessQueries userAccessQueries,
    ICustomerAccessQueries customerAccessQueries
) : IRequestHandler<CancelServiceCommand, Result>
{
    public async Task<Result> Handle(
        CancelServiceCommand request,
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

        if (service.IsTerminal && service.Status != ServiceStatus.Canceled)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        service.Cancel(request.Reason, request.CanceledAt);
        unitOfWork.Services.Update(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}