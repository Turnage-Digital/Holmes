using Holmes.Intake.Application.Gateways;

namespace Holmes.App.Server.Workflow;

public sealed class OrderWorkflowGateway(ILogger<OrderWorkflowGateway> logger)
    : IOrderWorkflowGateway
{
    public Task<OrderPolicyCheckResult> ValidateSubmissionAsync(
        OrderIntakeSubmission submission,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Validated intake submission for Order {OrderId} Session {SessionId}",
            submission.OrderId,
            submission.IntakeSessionId);

        // Phase 2 stub: always allow until workflow policies are implemented.
        return Task.FromResult(OrderPolicyCheckResult.Allowed());
    }

    public Task NotifyIntakeAcceptedAsync(
        OrderIntakeSubmission submission,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Order {OrderId} intake session {SessionId} accepted",
            submission.OrderId,
            submission.IntakeSessionId);
        return Task.CompletedTask;
    }
}