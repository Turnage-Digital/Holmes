using Holmes.Core.Domain;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed class GetServiceFulfillmentQueueQueryHandler(
    IServiceQueries serviceQueries
) : IRequestHandler<GetServiceFulfillmentQueueQuery, Result<ServiceFulfillmentQueuePagedResult>>
{
    public async Task<Result<ServiceFulfillmentQueuePagedResult>> Handle(
        GetServiceFulfillmentQueueQuery request,
        CancellationToken cancellationToken
    )
    {
        var result = await serviceQueries.GetFulfillmentQueuePagedAsync(
            request.Filter,
            request.Page,
            request.PageSize,
            cancellationToken);

        return Result.Success(result);
    }
}