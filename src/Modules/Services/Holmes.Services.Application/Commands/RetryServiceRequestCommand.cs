using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed record RetryServiceRequestCommand(
    UlidId ServiceRequestId,
    DateTimeOffset RetriedAt
) : RequestBase<Result>;

public sealed class RetryServiceRequestCommandHandler(
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<RetryServiceRequestCommand, Result>
{
    public async Task<Result> Handle(
        RetryServiceRequestCommand request,
        CancellationToken cancellationToken
    )
    {
        var serviceRequest = await unitOfWork.ServiceRequests.GetByIdAsync(
            request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
        {
            return Result.Fail($"Service request {request.ServiceRequestId} not found");
        }

        if (!serviceRequest.CanRetry)
        {
            return Result.Fail("Service request cannot be retried");
        }

        serviceRequest.Retry(request.RetriedAt);
        unitOfWork.ServiceRequests.Update(serviceRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}