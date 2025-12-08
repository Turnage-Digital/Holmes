using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceRequestQuery(
    UlidId ServiceRequestId
) : RequestBase<Result<ServiceRequestSummaryDto>>;

public sealed class GetServiceRequestQueryHandler(
    IServiceRequestQueries serviceRequestQueries
) : IRequestHandler<GetServiceRequestQuery, Result<ServiceRequestSummaryDto>>
{
    public async Task<Result<ServiceRequestSummaryDto>> Handle(
        GetServiceRequestQuery request,
        CancellationToken cancellationToken
    )
    {
        var serviceRequest = await serviceRequestQueries.GetByIdAsync(
            request.ServiceRequestId.ToString(), cancellationToken);

        if (serviceRequest is null)
        {
            return Result.Fail<ServiceRequestSummaryDto>($"Service request {request.ServiceRequestId} not found");
        }

        return Result.Success(serviceRequest);
    }
}