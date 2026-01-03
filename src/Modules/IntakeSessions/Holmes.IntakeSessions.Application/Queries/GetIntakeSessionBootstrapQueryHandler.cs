using Holmes.IntakeSessions.Contracts;
using Holmes.IntakeSessions.Contracts.Dtos;
using MediatR;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed class GetIntakeSessionBootstrapQueryHandler(IIntakeSessionQueries intakeSessionQueries)
    : IRequestHandler<GetIntakeSessionBootstrapQuery, IntakeSessionBootstrapDto?>
{
    public async Task<IntakeSessionBootstrapDto?> Handle(
        GetIntakeSessionBootstrapQuery request,
        CancellationToken cancellationToken
    )
    {
        return await intakeSessionQueries.GetBootstrapAsync(
            request.IntakeSessionId,
            request.ResumeToken,
            cancellationToken);
    }
}