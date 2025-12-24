using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed record RetryServiceCommand(
    UlidId ServiceId,
    DateTimeOffset RetriedAt
) : RequestBase<Result>;

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