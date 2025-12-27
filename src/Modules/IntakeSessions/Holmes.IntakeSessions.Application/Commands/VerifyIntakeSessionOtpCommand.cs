using Holmes.Core.Application;
using Holmes.Core.Domain;
using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed record VerifyIntakeSessionOtpCommand(
    UlidId IntakeSessionId,
    string Code
) : RequestBase<Result>;

public sealed class VerifyIntakeSessionOtpCommandHandler(
    IIntakeSessionsUnitOfWork unitOfWork
) : IRequestHandler<VerifyIntakeSessionOtpCommand, Result>
{
    public async Task<Result> Handle(VerifyIntakeSessionOtpCommand request, CancellationToken cancellationToken)
    {
        var session = await unitOfWork.IntakeSessions.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return Result.Fail($"Intake session '{request.IntakeSessionId}' not found.");
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Result.Fail("This invite has expired. Request a new link.");
        }

        if (!string.Equals(session.ResumeToken, request.Code, StringComparison.Ordinal))
        {
            return Result.Fail("Invalid or expired verification code.");
        }

        return Result.Success();
    }
}