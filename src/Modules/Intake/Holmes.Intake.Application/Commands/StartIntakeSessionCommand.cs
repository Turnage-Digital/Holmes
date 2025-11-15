using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain;
using Holmes.Workflow.Application.Commands;
using MediatR;

namespace Holmes.Intake.Application.Commands;

public sealed record StartIntakeSessionCommand(
    UlidId IntakeSessionId,
    string ResumeToken,
    DateTimeOffset StartedAt,
    string? DeviceInfo
) : RequestBase<Result>;

public sealed class StartIntakeSessionCommandHandler(
    IIntakeUnitOfWork unitOfWork,
    ISender sender
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

        await sender.Send(new MarkOrderIntakeStartedCommand(
            session.OrderId,
            request.IntakeSessionId,
            request.StartedAt,
            "Subject resumed intake"), cancellationToken);

        return Result.Success();
    }
}
