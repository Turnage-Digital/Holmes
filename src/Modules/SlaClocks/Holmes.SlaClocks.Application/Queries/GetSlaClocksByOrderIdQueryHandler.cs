using Holmes.Core.Domain;
using Holmes.SlaClocks.Contracts;
using Holmes.SlaClocks.Contracts.Dtos;
using MediatR;

namespace Holmes.SlaClocks.Application.Queries;

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