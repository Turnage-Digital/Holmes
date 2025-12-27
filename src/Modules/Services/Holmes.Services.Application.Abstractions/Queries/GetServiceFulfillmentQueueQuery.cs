using Holmes.Core.Application;
using Holmes.Core.Domain;

namespace Holmes.Services.Application.Abstractions.Queries;

public sealed record GetServiceFulfillmentQueueQuery(
    ServiceFulfillmentQueueFilter Filter,
    int Page,
    int PageSize
) : RequestBase<Result<ServiceFulfillmentQueuePagedResult>>;