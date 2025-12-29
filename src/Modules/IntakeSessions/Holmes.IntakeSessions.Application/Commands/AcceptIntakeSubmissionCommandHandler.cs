using Holmes.Core.Domain;
using Holmes.IntakeSessions.Application.Commands;
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
            return Result.Fail($"Intake session '{request.IntakeSessionId}' not found.");
        }

        if (session.ConsentArtifact is null)
        {
            return Result.Fail("Intake session has no consent artifact to attach to the order.");
        }

        session.AcceptSubmission(request.AcceptedAt);
        await repository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
