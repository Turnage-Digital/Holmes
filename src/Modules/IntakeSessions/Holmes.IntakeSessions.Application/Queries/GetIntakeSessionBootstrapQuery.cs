using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Application.Abstractions;
using Holmes.IntakeSessions.Application.Abstractions.Dtos;
using MediatR;

namespace Holmes.IntakeSessions.Application.Queries;

public sealed record GetIntakeSessionBootstrapQuery(
    UlidId IntakeSessionId,
    string ResumeToken
) : RequestBase<IntakeSessionBootstrapDto?>;

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