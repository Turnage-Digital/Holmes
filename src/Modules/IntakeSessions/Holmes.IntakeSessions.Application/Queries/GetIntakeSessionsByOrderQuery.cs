using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.IntakeSessions.Contracts.Dtos;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionsByOrderQuery(
    string OrderId
) : RequestBase<Result<IReadOnlyList<IntakeSessionSummaryDto>>>;