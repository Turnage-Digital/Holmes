using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record ResumeSlaClockCommand(
    UlidId ClockId,
    DateTimeOffset ResumedAt
) : RequestBase<Result>;

public sealed class ResumeSlaClockCommandHandler(
    ISlaClockUnitOfWork unitOfWork
) : IRequestHandler<ResumeSlaClockCommand, Result>
{
    public async Task<Result> Handle(ResumeSlaClockCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.SlaClocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            return Result.Fail($"SLA clock '{request.ClockId}' not found.");
        }

        clock.Resume(request.ResumedAt);
        await unitOfWork.SlaClocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}