using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Domain;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceRequestsByOrderQuery(
    UlidId OrderId
) : RequestBase<Result<IReadOnlyList<ServiceRequest>>>;

public sealed class GetServiceRequestsByOrderQueryHandler(
    IServicesUnitOfWork unitOfWork
) : IRequestHandler<GetServiceRequestsByOrderQuery, Result<IReadOnlyList<ServiceRequest>>>
{
    public async Task<Result<IReadOnlyList<ServiceRequest>>> Handle(
        GetServiceRequestsByOrderQuery request,
        CancellationToken cancellationToken
    )
    {
        var serviceRequests = await unitOfWork.ServiceRequests.GetByOrderIdAsync(
            request.OrderId, cancellationToken);

        return Result.Success(serviceRequests);
    }
}