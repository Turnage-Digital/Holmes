using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.SlaClocks.Contracts.Dtos;

namespace Holmes.SlaClocks.Application.Queries;

public sealed record GetSlaClocksByOrderIdQuery(
    string OrderId
) : RequestBase<Result<IReadOnlyList<SlaClockDto>>>;