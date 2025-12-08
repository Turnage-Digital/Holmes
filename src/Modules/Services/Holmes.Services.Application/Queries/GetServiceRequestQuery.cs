using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceRequestQuery(
    UlidId ServiceRequestId
) : RequestBase<Result<ServiceRequest>>;

public sealed class GetServiceRequestQueryHandler(
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<GetServiceRequestQuery, Result<ServiceRequest>>
{
    public async Task<Result<ServiceRequest>> Handle(
        GetServiceRequestQuery request,
        CancellationToken cancellationToken)
    {
        var serviceRequest = await unitOfWork.ServiceRequests.GetByIdAsync(
            request.ServiceRequestId, cancellationToken);

        if (serviceRequest is null)
        {
            return Result.Fail<ServiceRequest>($"Service request {request.ServiceRequestId} not found");
        }

        return Result.Success(serviceRequest);
    }
}
