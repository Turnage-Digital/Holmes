using Holmes.Core.Application;
using Holmes.Core.Contracts;
using Holmes.IntakeSessions.Contracts.Dtos;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionByIdQuery(
    string IntakeSessionId
) : RequestBase<Result<IntakeSessionSummaryDto>>;