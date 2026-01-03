using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.SlaClocks.Contracts.Dtos;

namespace Holmes.SlaClocks.Application.Queries;

public sealed record GetSlaClockByIdQuery(
    string ClockId
) : RequestBase<Result<SlaClockDto>>;