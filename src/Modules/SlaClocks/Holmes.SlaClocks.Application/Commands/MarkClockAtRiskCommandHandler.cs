using Holmes.Core.Application;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed class MarkClockAtRiskCommandHandler(
    ISlaClocksUnitOfWork unitOfWork
) : IRequestHandler<MarkClockAtRiskCommand, Result>
{
    public async Task<Result> Handle(MarkClockAtRiskCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.SlaClocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        clock.MarkAtRisk(request.AtRiskAt);
        await unitOfWork.SlaClocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}