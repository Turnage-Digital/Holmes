using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Commands;

public sealed record CancelServiceRequestCommand(
    UlidId ServiceRequestId,
    string Reason,
    DateTimeOffset CanceledAt
) : RequestBase<Result>;

public sealed class CancelServiceRequestCommandHandler(
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<CancelServiceRequestCommand, Result>
{
    public async Task<Result> Handle(
        CancelServiceRequestCommand request,
        CancellationToken cancellationToken
    )
    {
        var serviceRequest = await unitOfWork.ServiceRequests.GetByIdAsync(
            request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
        {
            return Result.Fail($"Service request {request.ServiceRequestId} not found");
        }

        if (serviceRequest.IsTerminal && serviceRequest.Status != ServiceStatus.Canceled)
        {
            return Result.Fail("Service request is already in a terminal state and cannot be canceled");
        }

        serviceRequest.Cancel(request.Reason, request.CanceledAt);
        unitOfWork.ServiceRequests.Update(serviceRequest);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}