using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record MarkClockBreachedCommand(
    UlidId ClockId,
    DateTimeOffset BreachedAt
) : RequestBase<Result>, ISkipUserAssignment;

public sealed class MarkClockBreachedCommandHandler(
    ISlaClocksUnitOfWork unitOfWork
) : IRequestHandler<MarkClockBreachedCommand, Result>
{
    public async Task<Result> Handle(MarkClockBreachedCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.SlaClocks.GetByIdAsync(request.ClockId, cancellationToken);
        if (clock is null)
        {
            return Result.Fail($"SLA clock '{request.ClockId}' not found.");
        }

        clock.MarkBreached(request.BreachedAt);
        await unitOfWork.SlaClocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}