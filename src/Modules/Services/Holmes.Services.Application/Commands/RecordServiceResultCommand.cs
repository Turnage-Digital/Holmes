using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed record RecordServiceResultCommand(
    UlidId ServiceRequestId,
    ServiceResult Result,
    DateTimeOffset CompletedAt
) : RequestBase<Result>;

public sealed class RecordServiceResultCommandHandler(
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<RecordServiceResultCommand, Result>
{
    public async Task<Result> Handle(
        RecordServiceResultCommand request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await unitOfWork.ServiceRequests.GetByIdAsync(
            request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
        {
            return Result.Fail($"Service request {request.ServiceRequestId} not found");
        }

        if (serviceRequest.IsTerminal)
        {
            return Result.Fail("Service request is already in a terminal state");
        }

        serviceRequest.RecordResult(request.Result, request.CompletedAt);
        unitOfWork.ServiceRequests.Update(serviceRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
