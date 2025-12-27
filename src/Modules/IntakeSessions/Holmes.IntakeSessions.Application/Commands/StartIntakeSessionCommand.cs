using Holmes.Core.Domain;
using Holmes.IntakeSessions.Application.Abstractions.Commands;
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
            return Result.Fail($"Intake session '{request.IntakeSessionId}' not found.");
        }

        if (!string.Equals(session.ResumeToken, request.ResumeToken, StringComparison.Ordinal))
        {
            return Result.Fail("Resume token is invalid.");
        }

        try
        {
            session.Start(request.StartedAt, request.DeviceInfo);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await unitOfWork.IntakeSessions.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        // IntakeSessionStarted domain event is published by UnitOfWork.SaveChangesAsync
        // IntakeToWorkflowHandler in App.Integration listens and sends MarkOrderIntakeStartedCommand

        return Result.Success();
    }
}
