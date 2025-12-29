using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.SlaClocks.Application.Abstractions.Dtos;

namespace Holmes.SlaClocks.Application.Queries;

public sealed record GetSlaClockByIdQuery(
    string ClockId
) : RequestBase<Result<SlaClockDto>>;