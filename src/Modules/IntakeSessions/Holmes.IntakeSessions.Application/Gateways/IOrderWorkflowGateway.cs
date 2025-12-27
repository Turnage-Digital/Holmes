using Holmes.Core.Domain.ValueObjects;
using Holmes.IntakeSessions.Domain.ValueObjects;

namespace Holmes.IntakeSessions.Application.Gateways;

public interface IOrderWorkflowGateway
{
    Task<OrderPolicyCheckResult> ValidateSubmissionAsync(
        OrderIntakeSubmission submission,
        CancellationToken cancellationToken
    );

    Task NotifyIntakeSubmittedAsync(
        OrderIntakeSubmission submission,
        DateTimeOffset submittedAt,
        CancellationToken cancellationToken
    );

    Task NotifyIntakeAcceptedAsync(
        OrderIntakeSubmission submission,
        DateTimeOffset acceptedAt,
        CancellationToken cancellationToken
    );
}

public sealed record OrderIntakeSubmission(
    UlidId OrderId,
    UlidId SubjectId,
    UlidId IntakeSessionId,
    PolicySnapshot PolicySnapshot,
    ConsentArtifactPointer ConsentArtifact
);

public sealed record OrderPolicyCheckResult(bool IsAllowed, string? FailureReason = null)
{
    public static OrderPolicyCheckResult Allowed()
    {
        return new OrderPolicyCheckResult(true);
    }

    public static OrderPolicyCheckResult Blocked(string reason)
    {
        return new OrderPolicyCheckResult(false, reason);
    }
}