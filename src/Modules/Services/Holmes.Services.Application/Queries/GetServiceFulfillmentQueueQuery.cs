using Holmes.Core.Contracts;
using Holmes.Core.Domain;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceFulfillmentQueueQuery(
    ServiceFulfillmentQueueFilter Filter,
    int Page,
    int PageSize
) : RequestBase<Result<ServiceFulfillmentQueuePagedResult>>;