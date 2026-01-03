using Holmes.Core.Application;
using Holmes.Core.Contracts;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceFulfillmentQueueQuery(
    ServiceFulfillmentQueueFilter Filter,
    int Page,
    int PageSize
) : RequestBase<Result<ServiceFulfillmentQueuePagedResult>>;