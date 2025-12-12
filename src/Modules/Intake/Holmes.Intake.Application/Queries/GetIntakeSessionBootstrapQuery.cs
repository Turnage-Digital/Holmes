using Holmes.Core.Application;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Abstractions.Dtos;
using Holmes.Intake.Application.Abstractions.Queries;
using MediatR;

namespace Holmes.Intake.Application.Queries;

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