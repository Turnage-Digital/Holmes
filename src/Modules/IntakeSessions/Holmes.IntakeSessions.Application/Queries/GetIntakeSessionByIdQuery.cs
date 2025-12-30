using Holmes.Core.Contracts;
using Holmes.Core.Domain;
using Holmes.IntakeSessions.Contracts.Dtos;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionByIdQuery(
    string IntakeSessionId
) : RequestBase<Result<IntakeSessionSummaryDto>>;