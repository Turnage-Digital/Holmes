using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record MarkClockBreachedCommand(
    UlidId ClockId,
    DateTimeOffset BreachedAt
) : RequestBase<Result>;

public sealed class MarkClockBreachedCommandHandler(
    ISlaClockUnitOfWork unitOfWork
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
        unitOfWork.SlaClocks.Update(clock);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}