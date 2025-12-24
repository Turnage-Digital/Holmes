using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.IntakeSessions.Application.Abstractions.Dtos;
using Holmes.IntakeSessions.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionsByOrderQuery(
    string OrderId
) : RequestBase<Result<IReadOnlyList<IntakeSessionSummaryDto>>>;

public sealed class GetIntakeSessionsByOrderQueryHandler(
    IIntakeSessionQueries intakeSessionQueries
) : IRequestHandler<GetIntakeSessionsByOrderQuery, Result<IReadOnlyList<IntakeSessionSummaryDto>>>
{
    public async Task<Result<IReadOnlyList<IntakeSessionSummaryDto>>> Handle(
        GetIntakeSessionsByOrderQuery request,
        CancellationToken cancellationToken
    )
    {
        var sessions = await intakeSessionQueries.GetByOrderIdAsync(
            request.OrderId, cancellationToken);

        return Result.Success(sessions);
    }
}