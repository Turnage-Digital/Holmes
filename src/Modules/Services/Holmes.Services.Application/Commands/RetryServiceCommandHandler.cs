using Holmes.Core.Domain;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed class RetryServiceCommandHandler(
    IServicesUnitOfWork unitOfWork
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
            return Result.Fail($"Service {request.ServiceId} not found");
        }

        if (!service.CanRetry)
        {
            return Result.Fail("Service cannot be retried");
        }

        service.Retry(request.RetriedAt);
        unitOfWork.Services.Update(service);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}