using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.SlaClocks.Application.Abstractions.Dtos;
using Holmes.SlaClocks.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.SlaClocks.Application.Queries;

public sealed record GetSlaClocksByOrderIdQuery(
    string OrderId
) : RequestBase<Result<IReadOnlyList<SlaClockDto>>>;

public sealed class GetSlaClocksByOrderIdQueryHandler(
    ISlaClockQueries slaClockQueries
) : IRequestHandler<GetSlaClocksByOrderIdQuery, Result<IReadOnlyList<SlaClockDto>>>
{
    public async Task<Result<IReadOnlyList<SlaClockDto>>> Handle(
        GetSlaClocksByOrderIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var clocks = await slaClockQueries.GetByOrderIdAsync(request.OrderId, cancellationToken);
        return Result.Success(clocks);
    }
}
