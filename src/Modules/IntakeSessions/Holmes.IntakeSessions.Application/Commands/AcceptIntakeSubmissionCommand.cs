using Holmes.Core.Domain;
using Holmes.IntakeSessions.Application.Abstractions.Commands;
using Holmes.IntakeSessions.Application.Gateways;
using Holmes.IntakeSessions.Domain;
using MediatR;

namespace Holmes.IntakeSessions.Application.Commands;

public sealed class AcceptIntakeSubmissionCommandHandler(
    IOrderWorkflowGateway orderWorkflowGateway,
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

        var submission = new OrderIntakeSubmission(
            session.OrderId,
            session.SubjectId,
            session.Id,
            session.PolicySnapshot,
            session.ConsentArtifact);

        session.AcceptSubmission(request.AcceptedAt);
        await repository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await orderWorkflowGateway.NotifyIntakeAcceptedAsync(submission, request.AcceptedAt, cancellationToken);

        return Result.Success();
    }
}
