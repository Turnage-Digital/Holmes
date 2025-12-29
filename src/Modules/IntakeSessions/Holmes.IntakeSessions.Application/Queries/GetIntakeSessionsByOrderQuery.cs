using Holmes.Core.Application.Abstractions;
using Holmes.Core.Domain;
using Holmes.IntakeSessions.Application.Abstractions.Dtos;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionsByOrderQuery(
    string OrderId
) : RequestBase<Result<IReadOnlyList<IntakeSessionSummaryDto>>>;