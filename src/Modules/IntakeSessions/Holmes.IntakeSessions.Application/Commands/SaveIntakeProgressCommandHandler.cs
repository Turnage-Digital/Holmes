using Holmes.Core.Domain;
using Holmes.IntakeSessions.Domain;
using Holmes.IntakeSessions.Domain.ValueObjects;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed class SaveIntakeProgressCommandHandler(IIntakeSessionsUnitOfWork unitOfWork)
    : IRequestHandler<SaveIntakeProgressCommand, Result>
{
    public async Task<Result> Handle(SaveIntakeProgressCommand request, CancellationToken cancellationToken)
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
            var snapshot = IntakeAnswersSnapshot.Create(
                request.SchemaVersion,
                request.PayloadHash,
                request.PayloadCipherText,
                request.UpdatedAt);
            session.SaveProgress(snapshot);
        }
        catch (InvalidOperationException ex)
        {
            return Result.Fail(ex.Message);
        }

        await unitOfWork.IntakeSessions.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}