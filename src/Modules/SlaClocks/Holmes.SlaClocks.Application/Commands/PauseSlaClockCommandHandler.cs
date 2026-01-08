using Holmes.Core.Application;
using Holmes.Customers.Contracts;
using Holmes.SlaClocks.Domain;
using Holmes.Users.Contracts;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed class PauseSlaClockCommandHandler(
    ISlaClocksUnitOfWork unitOfWork,
    IUserAccessQueries userAccessQueries,
    ICustomerAccessQueries customerAccessQueries
) : IRequestHandler<PauseSlaClockCommand, Result>
{
    public async Task<Result> Handle(PauseSlaClockCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.SlaClocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        var actor = request.GetUserUlid();
        var isGlobalAdmin = await userAccessQueries.IsGlobalAdminAsync(actor, cancellationToken);
        if (!isGlobalAdmin)
        {
            var allowedCustomers = await customerAccessQueries.GetAdminCustomerIdsAsync(actor, cancellationToken);
            if (!allowedCustomers.Contains(clock.CustomerId.ToString()))
            {
                return Result.Fail(ResultErrors.Forbidden);
            }
        }

        clock.Pause(request.Reason, request.PausedAt);
        await unitOfWork.SlaClocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}