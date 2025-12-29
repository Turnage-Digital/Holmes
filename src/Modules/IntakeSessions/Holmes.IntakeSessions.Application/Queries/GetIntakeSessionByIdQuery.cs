using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.IntakeSessions.Application.Abstractions.Dtos;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionByIdQuery(
    string IntakeSessionId
) : RequestBase<Result<IntakeSessionSummaryDto>>;