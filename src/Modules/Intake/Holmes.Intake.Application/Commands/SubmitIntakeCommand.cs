using Holmes.Core.Application;
using Holmes.Core.Domain.Results;
using Holmes.Core.Domain.ValueObjects;
using Holmes.Intake.Application.Gateways;
using Holmes.Intake.Domain;
using MediatR;

namespace Holmes.Intake.Application.Commands;

public sealed record SubmitIntakeCommand(
    UlidId IntakeSessionId,
    DateTimeOffset SubmittedAt
) : RequestBase<Result>;

public sealed class SubmitIntakeCommandHandler(
    IOrderWorkflowGateway orderWorkflowGateway,
    IIntakeUnitOfWork unitOfWork
)
    : IRequestHandler<SubmitIntakeCommand, Result>
{
    public async Task<Result> Handle(SubmitIntakeCommand request, CancellationToken cancellationToken)
    {
        var repository = unitOfWork.IntakeSessions;
        var session = await repository.GetByIdAsync(request.IntakeSessionId, cancellationToken);
        if (session is null)
        {
            return Result.Fail($"Intake session '{request.IntakeSessionId}' not found.");
        }

        if (session.ConsentArtifact is null || session.AnswersSnapshot is null)
        {
            return Result.Fail("Intake session is missing consent or answers data.");
        }

        var submission = new OrderIntakeSubmission(
            session.OrderId,
            session.SubjectId,
            session.Id,
            session.PolicySnapshot,
            session.ConsentArtifact);

        var policyResult = await orderWorkflowGateway.ValidateSubmissionAsync(submission, cancellationToken);
        if (!policyResult.IsAllowed)
        {
            var reason = policyResult.FailureReason ?? "Submission blocked by policy.";
            return Result.Fail(reason);
        }

        session.Submit(request.SubmittedAt);
        await repository.UpdateAsync(session, cancellationToken);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}