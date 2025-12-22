using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record PauseSlaClockCommand(
    UlidId ClockId,
    string Reason,
    DateTimeOffset PausedAt
) : RequestBase<Result>, ISkipUserAssignment;

public sealed class PauseSlaClockCommandHandler(
    ISlaClockUnitOfWork unitOfWork
) : IRequestHandler<PauseSlaClockCommand, Result>
{
    public async Task<Result> Handle(PauseSlaClockCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.SlaClocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            return Result.Fail($"SLA clock '{request.ClockId}' not found.");
        }

        clock.Pause(request.Reason, request.PausedAt);
        await unitOfWork.SlaClocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}