using Holmes.Core.Contracts;
using Holmes.Core.Application;
using Holmes.IntakeSessions.Contracts.Dtos;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionsByOrderQuery(
    string OrderId
) : RequestBase<Result<IReadOnlyList<IntakeSessionSummaryDto>>>;