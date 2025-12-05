using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record MarkClockAtRiskCommand(
    UlidId ClockId,
    DateTimeOffset AtRiskAt
) : RequestBase<Result>;

public sealed class MarkClockAtRiskCommandHandler(
    ISlaClockUnitOfWork unitOfWork
) : IRequestHandler<MarkClockAtRiskCommand, Result>
{
    public async Task<Result> Handle(MarkClockAtRiskCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.SlaClocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            return Result.Fail($"SLA clock '{request.ClockId}' not found.");
        }

        clock.MarkAtRisk(request.AtRiskAt);
        unitOfWork.SlaClocks.Update(clock);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
