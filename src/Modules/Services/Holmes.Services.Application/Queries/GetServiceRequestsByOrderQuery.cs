using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceRequestsByOrderQuery(
    UlidId OrderId
) : RequestBase<Result<IReadOnlyList<ServiceRequestSummaryDto>>>;

public sealed class GetServiceRequestsByOrderQueryHandler(
    IServiceRequestQueries serviceRequestQueries
) : IRequestHandler<GetServiceRequestsByOrderQuery, Result<IReadOnlyList<ServiceRequestSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<ServiceRequestSummaryDto>>> Handle(
        GetServiceRequestsByOrderQuery request,
        CancellationToken cancellationToken
    )
    {
        var serviceRequests = await serviceRequestQueries.GetByOrderIdAsync(
            request.OrderId.ToString(), cancellationToken);

        return Result.Success(serviceRequests);
    }
}