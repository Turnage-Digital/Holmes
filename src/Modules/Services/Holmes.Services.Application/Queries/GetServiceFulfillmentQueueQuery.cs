using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceFulfillmentQueueQuery(
    ServiceFulfillmentQueueFilter Filter,
    int Page,
    int PageSize
) : RequestBase<Result<ServiceFulfillmentQueuePagedResult>>;

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