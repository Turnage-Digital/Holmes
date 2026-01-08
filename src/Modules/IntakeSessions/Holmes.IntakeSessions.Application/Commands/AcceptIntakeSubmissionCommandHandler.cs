using Holmes.Core.Application;
using Holmes.IntakeSessions.Domain;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed class AcceptIntakeSubmissionCommandHandler(
    IIntakeSessionsUnitOfWork unitOfWork
)
    : IRequestHandler<AcceptIntakeSubmissionCommand, Result>
{
    public async Task<Result> Handle(AcceptIntakeSubmissionCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.IntakeSessions;
        var session = await repository.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return Result.Fail(ResultErrors.NotFound);
        }

        if (session.AuthorizationArtifact is null)
        {
            return Result.Fail(ResultErrors.Validation);
        }

        session.AcceptSubmission(request.AcceptedAt);
        await repository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
