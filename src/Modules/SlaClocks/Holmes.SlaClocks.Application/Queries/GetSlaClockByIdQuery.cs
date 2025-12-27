using Holmes.Core.Domain;
using Holmes.SlaClocks.Application.Abstractions;
using Holmes.SlaClocks.Application.Abstractions.Dtos;
using Holmes.SlaClocks.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.SlaClocks.Application.Queries;

public sealed class GetSlaClockByIdQueryHandler(
    ISlaClockQueries slaClockQueries
) : IRequestHandler<GetSlaClockByIdQuery, Result<SlaClockDto>>
{
    public async Task<Result<SlaClockDto>> Handle(
        GetSlaClockByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var clock = await slaClockQueries.GetByIdAsync(request.ClockId, cancellationToken);

        if (clock is null)
        {
            return Result.Fail<SlaClockDto>($"SLA clock '{request.ClockId}' not found.");
        }

        return Result.Success(clock);
    }
}