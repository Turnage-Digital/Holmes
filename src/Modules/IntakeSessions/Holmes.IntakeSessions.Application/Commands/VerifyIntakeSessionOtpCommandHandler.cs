using Holmes.Core.Application;
using Holmes.IntakeSessions.Domain;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed class VerifyIntakeSessionOtpCommandHandler(
    IIntakeSessionsUnitOfWork unitOfWork
) : IRequestHandler<VerifyIntakeSessionOtpCommand, Result>
{
    public async Task<Result> Handle(VerifyIntakeSessionOtpCommand request, CancellationToken cancellationToken)
    {
        var session = await unitOfWork.IntakeSessions.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        if (session.ExpiresAt <= DateTimeOffset.UtcNow)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        if (!string.Equals(session.ResumeToken, request.Code, StringComparison.Ordinal))
        {
            return Result.Fail(ResultErrors.Validation);
        }

        return Result.Success();
    }
}