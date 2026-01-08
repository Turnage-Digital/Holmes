using Holmes.Core.Application;
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
            return Result.Fail(ResultErrors.NotFound);
        }

        if (!string.Equals(session.ResumeToken, request.ResumeToken, StringComparison.Ordinal))
        {
            return Result.Fail(ResultErrors.Validation);
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
        catch (InvalidOperationException)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        await unitOfWork.IntakeSessions.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}