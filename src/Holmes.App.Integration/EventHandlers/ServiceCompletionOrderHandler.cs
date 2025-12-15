using Holmes.Services.Application.Abstractions.Queries;
using Holmes.Services.Domain.Events;
using Holmes.Workflow.Application.Abstractions.Queries;
using Holmes.Workflow.Application.Commands;
using Holmes.Workflow.Domain;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Holmes.App.Integration.EventHandlers;

/// <summary>
///     Handles ServiceRequestCompleted events to check if all services for an order
///     are complete, and if so, transitions the order to ReadyForReport.
/// </summary>
public sealed class ServiceCompletionOrderHandler(
    IServiceRequestQueries serviceRequestQueries,
    IOrderQueries orderQueries,
    ISender sender,
    ILogger<ServiceCompletionOrderHandler> logger
) : INotificationHandler<ServiceRequestCompleted>
{
    public async Task Handle(ServiceRequestCompleted notification, CancellationToken cancellationToken)
    {
        logger.LogDebug(
            "Service {ServiceRequestId} completed for Order {OrderId} with status {ResultStatus}",
            notification.ServiceRequestId,
            notification.OrderId,
            notification.ResultStatus);

        // Get the order's current status to verify it's in FulfillmentInProgress
        var orderSummary = await orderQueries.GetSummaryByIdAsync(
            notification.OrderId.ToString(),
            cancellationToken);

        if (orderSummary is null)
        {
            logger.LogWarning(
                "Order {OrderId} not found when processing service completion",
                notification.OrderId);
            return;
        }

        // Only proceed if order is in FulfillmentInProgress
        if (orderSummary.Status != OrderStatus.FulfillmentInProgress.ToString())
        {
            logger.LogDebug(
                "Order {OrderId} is in {Status}, not FulfillmentInProgress. Skipping completion check.",
                notification.OrderId,
                orderSummary.Status);
            return;
        }

        // Check if all services for the order are completed
        var completionStatus = await serviceRequestQueries.GetOrderCompletionStatusAsync(
            notification.OrderId.ToString(),
            cancellationToken);

        logger.LogDebug(
            "Order {OrderId} service completion: {Completed}/{Total} completed, {Pending} pending, {Failed} failed",
            notification.OrderId,
            completionStatus.CompletedServices,
            completionStatus.TotalServices,
            completionStatus.PendingServices,
            completionStatus.FailedServices);

        if (!completionStatus.AllCompleted)
        {
            logger.LogDebug(
                "Order {OrderId} has {Pending} pending services, not ready for report",
                notification.OrderId,
                completionStatus.PendingServices);
            return;
        }

        // All services are completed, transition order to ReadyForReport
        logger.LogInformation(
            "All {Count} services completed for Order {OrderId}, transitioning to ReadyForReport",
            completionStatus.CompletedServices,
            notification.OrderId);

        var command = new MarkOrderReadyForReportCommand(
            notification.OrderId,
            notification.CompletedAt,
            $"All {completionStatus.CompletedServices} services completed");

        var result = await sender.Send(command, cancellationToken);

        if (result.IsSuccess)
        {
            logger.LogInformation(
                "Order {OrderId} transitioned to ReadyForReport",
                notification.OrderId);
        }
        else
        {
            logger.LogError(
                "Failed to transition Order {OrderId} to ReadyForReport: {Error}",
                notification.OrderId,
                result.Error);
        }
    }
}