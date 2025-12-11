using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Intake.Application.Abstractions.Dtos;
using Holmes.Intake.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Intake.Application.Queries;

public sealed record GetIntakeSessionByIdQuery(
    string IntakeSessionId
) : RequestBase<Result<IntakeSessionSummaryDto>>;

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