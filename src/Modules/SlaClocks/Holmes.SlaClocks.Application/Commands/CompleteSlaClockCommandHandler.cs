using Holmes.Core.Domain;
using Holmes.SlaClocks.Domain;
using MediatR;

namespace Holmes.SlaClocks.Application.Commands;

public sealed class CompleteSlaClockCommandHandler(
    ISlaClocksUnitOfWork unitOfWork
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
        await unitOfWork.SlaClocks.UpdateAsync(clock, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}