using Holmes.Core.Domain;
using Holmes.IntakeSessions.Application.Abstractions;
using Holmes.IntakeSessions.Application.Abstractions.Dtos;
using MediatR;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed class GetIntakeSessionByIdQueryHandler(
    IIntakeSessionQueries intakeSessionQueries
) : IRequestHandler<GetIntakeSessionByIdQuery, Result<IntakeSessionSummaryDto>>
{
    public async Task<Result<IntakeSessionSummaryDto>> Handle(
        GetIntakeSessionByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        var session = await intakeSessionQueries.GetByIdAsync(
            request.IntakeSessionId, cancellationToken);

        if (session is null)
        {
            return Result.Fail<IntakeSessionSummaryDto>($"Intake session {request.IntakeSessionId} not found");
        }

        return Result.Success(session);
    }
}