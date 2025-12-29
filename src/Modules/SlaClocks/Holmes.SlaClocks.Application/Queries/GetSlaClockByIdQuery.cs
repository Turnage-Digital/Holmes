using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.SlaClocks.Application.Abstractions.Dtos;

namespace Holmes.SlaClocks.Application.Queries;

public sealed record GetSlaClockByIdQuery(
    string ClockId
) : RequestBase<Result<SlaClockDto>>;