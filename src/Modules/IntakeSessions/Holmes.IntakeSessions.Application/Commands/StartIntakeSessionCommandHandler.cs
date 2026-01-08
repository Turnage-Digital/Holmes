using Holmes.Core.Application;
using Holmes.IntakeSessions.Domain;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed class StartIntakeSessionCommandHandler(
    IIntakeSessionsUnitOfWork unitOfWork
) : IRequestHandler<StartIntakeSessionCommand, Result>
{
    public async Task<Result> Handle(StartIntakeSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await unitOfWork.IntakeSessions.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        if (!string.Equals(session.ResumeToken, request.ResumeToken, StringComparison.Ordinal))
        {
            return Result.Fail(ResultErrors.Validation);
        }

        try
        {
            session.Start(request.StartedAt, request.DeviceInfo);
        }
        catch (InvalidOperationException)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        await unitOfWork.IntakeSessions.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // IntakeSessionStarted domain event is persisted by UnitOfWork.SaveChangesAsync
        // IntakeSessionOrderHandler listens and sends MarkOrderIntakeStartedCommand

        return Result.Success();
    }
}
