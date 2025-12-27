using Holmes.Core.Domain;
using Holmes.Services.Application.Abstractions.Dtos;
using Holmes.Services.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Services.Application.Queries;

public sealed class GetServicesByOrderQueryHandler(
    IServiceQueries serviceQueries
) : IRequestHandler<GetServicesByOrderQuery, Result<IReadOnlyList<ServiceSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<ServiceSummaryDto>>> Handle(
        GetServicesByOrderQuery request,
        CancellationToken cancellationToken
    )
    {
        var services = await serviceQueries.GetByOrderIdAsync(
            request.OrderId.ToString(), cancellationToken);

        return Result.Success(services);
    }
}