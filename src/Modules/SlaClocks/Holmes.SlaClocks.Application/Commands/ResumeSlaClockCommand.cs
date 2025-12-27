using Holmes.Core.Domain;
using Holmes.SlaClocks.Application.Abstractions.Commands;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed class ResumeSlaClockCommandHandler(
    ISlaClocksUnitOfWork unitOfWork
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