using Holmes.IntakeSessions.Application.Gateways;
using Holmes.Orders.Application.Commands;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Integration.Gateways;

public sealed class OrderWorkflowGateway(
    ISender sender,
    ILogger<OrderWorkflowGateway> logger
) : IOrderWorkflowGateway
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

    public async Task NotifyIntakeSubmittedAsync(
        OrderIntakeSubmission submission,
        DateTimeOffset submittedAt,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Order {OrderId} intake session {SessionId} submitted",
            submission.OrderId,
            submission.IntakeSessionId);

        var command = new MarkOrderIntakeSubmittedCommand(
            submission.OrderId,
            submission.IntakeSessionId,
            submittedAt,
            "Intake submission received");

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            var error = result.Error ?? "Failed to record intake submission on order.";
            logger.LogWarning("Unable to mark Order {OrderId} submitted: {Error}", submission.OrderId, error);
            throw new InvalidOperationException(error);
        }
    }

    public async Task NotifyIntakeAcceptedAsync(
        OrderIntakeSubmission submission,
        DateTimeOffset acceptedAt,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Order {OrderId} intake session {SessionId} accepted",
            submission.OrderId,
            submission.IntakeSessionId);

        var command = new MarkOrderReadyForFulfillmentCommand(
            submission.OrderId,
            acceptedAt,
            "Intake accepted");

        var result = await sender.Send(command, cancellationToken);
        if (!result.IsSuccess)
        {
            var error = result.Error ?? "Failed to advance order to ready_for_fulfillment.";
            logger.LogWarning("Unable to advance Order {OrderId}: {Error}", submission.OrderId, error);
            throw new InvalidOperationException(error);
        }
    }
}