using Holmes.Core.Application;
using Holmes.IntakeSessions.Domain;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed class RecordDisclosureViewedCommandHandler(
    IIntakeSessionsUnitOfWork unitOfWork
) : IRequestHandler<RecordDisclosureViewedCommand, Result>
{
    public async Task<Result> Handle(RecordDisclosureViewedCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.IntakeSessions;
        var session = await repository.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        session.RecordDisclosureViewed(request.ViewedAt);
        await repository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
