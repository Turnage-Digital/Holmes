using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Domain;
using Holmes.Intake.Domain.ValueObjects;
using MediatR;

namespace Holmes.Intake.Application.Commands;

public sealed record SaveIntakeProgressCommand(
    UlidId IntakeSessionId,
    string ResumeToken,
    string SchemaVersion,
    string PayloadHash,
    string PayloadCipherText,
    DateTimeOffset UpdatedAt
) : RequestBase<Result>;

public sealed class SaveIntakeProgressCommandHandler(IIntakeUnitOfWork unitOfWork)
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