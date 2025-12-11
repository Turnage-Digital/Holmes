using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed record CompleteSlaClockCommand(
    UlidId OrderId,
    ClockKind Kind,
    DateTimeOffset CompletedAt
) : RequestBase<Result>;

public sealed class CompleteSlaClockCommandHandler(
    ISlaClockUnitOfWork unitOfWork
) : IRequestHandler<CompleteSlaClockCommand, Result>
{
    public async Task<Result> Handle(CompleteSlaClockCommand request, CancellationToken cancellationToken)
    {
        var clock = await unitOfWork.SlaClocks.GetByOrderIdAndKindAsync(
            request.OrderId, request.Kind, cancellationToken);

        if (clock is null)
        {
            // No clock to complete - this is fine
            return Result.Success();
        }

        clock.Complete(request.CompletedAt);
        unitOfWork.SlaClocks.Update(clock);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}