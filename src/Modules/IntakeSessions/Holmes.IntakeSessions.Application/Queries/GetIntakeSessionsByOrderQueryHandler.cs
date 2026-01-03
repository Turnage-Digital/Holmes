using Holmes.Core.Application;
using Holmes.IntakeSessions.Contracts;
using Holmes.IntakeSessions.Contracts.Dtos;
using MediatR;

namespace Holmes.IntakeSessions.Application.Queries;

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