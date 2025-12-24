using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed record GetServiceQuery(
    UlidId ServiceId
) : RequestBase<Result<ServiceSummaryDto>>;

public sealed class GetServiceQueryHandler(
    IServiceQueries serviceQueries
) : IRequestHandler<GetServiceQuery, Result<ServiceSummaryDto>>
{
    public async Task<Result<ServiceSummaryDto>> Handle(
        GetServiceQuery request,
        CancellationToken cancellationToken
    )
    {
        var service = await serviceQueries.GetByIdAsync(
            request.ServiceId.ToString(), cancellationToken);

        if (service is null)
        {
            return Result.Fail<ServiceSummaryDto>($"Service {request.ServiceId} not found");
        }

        return Result.Success(service);
    }
}